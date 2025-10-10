using System.Text;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Services;

public interface ITestScenarioService
{
    Task<TestScenarioResponse> GenerateScenarioTestAsync(TestScenarioRequest request);
    Task<TestScenarioResponse> AnalyzeAndBuildScenarioAsync(TestScenarioRequest request);
    Task<object> GeneratePlaywrightJsonAsync(TestScenarioRequest request);
}

public class TestScenarioService : ITestScenarioService
{
    private readonly IPageAnalyzerService _pageAnalyzer;
    private readonly ILogger<TestScenarioService> _logger;
    private readonly BrowserAutomationService _browserAutomation;

    public TestScenarioService(
        IPageAnalyzerService pageAnalyzer,
        ILogger<TestScenarioService> logger,
        BrowserAutomationService browserAutomation)
    {
        _pageAnalyzer = pageAnalyzer;
        _logger = logger;
        _browserAutomation = browserAutomation;
    }

    public async Task<TestScenarioResponse> AnalyzeAndBuildScenarioAsync(TestScenarioRequest request)
    {
        var response = new TestScenarioResponse
        {
            ScenarioName = request.ScenarioName
        };

        // Analyze each page in the scenario
        foreach (var step in request.Steps.OrderBy(s => s.Order))
        {
            _logger.LogInformation("Analyzing step {Order}: {PageName}", step.Order, step.PageName);

            AnalyzePageResponse pageAnalysis;
            
            if (!string.IsNullOrEmpty(step.HtmlContent))
            {
                pageAnalysis = await _pageAnalyzer.AnalyzeHtmlAsync(step.HtmlContent);
            }
            else if (!string.IsNullOrEmpty(step.PageUrl))
            {
                pageAnalysis = await _pageAnalyzer.AnalyzeUrlAsync(step.PageUrl);
            }
            else
            {
                _logger.LogWarning("Step {Order} has no HTML or URL", step.Order);
                continue;
            }

            response.AnalyzedSteps.Add(new PageStepResult
            {
                Order = step.Order,
                PageName = step.PageName,
                AvailableElements = pageAnalysis.Elements,
                ConfiguredActions = step.Actions
            });
        }

        // Generate the test script
        response.GeneratedScript = GenerateEndToEndScript(request, response.AnalyzedSteps);
        response.Message = $"Successfully generated end-to-end test with {request.Steps.Count} steps";

        return response;
    }

    public Task<TestScenarioResponse> GenerateScenarioTestAsync(TestScenarioRequest request)
    {
        // For pre-configured scenarios (no analysis needed)
        var response = new TestScenarioResponse
        {
            ScenarioName = request.ScenarioName,
            GeneratedScript = GenerateEndToEndScript(request, new List<PageStepResult>()),
            Message = $"Successfully generated test scenario: {request.ScenarioName}"
        };

        return Task.FromResult(response);
    }

    public async Task<object> GeneratePlaywrightJsonAsync(TestScenarioRequest request)
    {
        var steps = new List<object>();

        // Use sequential browser session to navigate through pages
        if (request.Steps != null && request.Steps.Any())
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            int order = 1;
            bool isFirstPage = true;
            bool isAuthenticated = false;
            
            foreach (var step in request.Steps.OrderBy(s => s.Order))
            {
                if (!string.IsNullOrEmpty(step.PageUrl))
                {
                    _logger.LogInformation("Processing step {Order}: {PageName} - {Url}", step.Order, step.PageName, step.PageUrl);
                    
                    // Check if this is a login page
                    bool isLoginPage = step.PageUrl.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                                      step.PageUrl.Contains("signin", StringComparison.OrdinalIgnoreCase);
                    
                    // Determine the actual target URL
                    var targetUrl = step.PageUrl;
                    if (step.PageUrl.Contains("rURL="))
                    {
                        var rUrlMatch = System.Text.RegularExpressions.Regex.Match(step.PageUrl, @"rURL=([^&]+)");
                        if (rUrlMatch.Success)
                        {
                            targetUrl = System.Web.HttpUtility.UrlDecode(rUrlMatch.Groups[1].Value);
                            _logger.LogInformation("Extracted target URL from redirect: {TargetUrl}", targetUrl);
                        }
                    }
                    
                    // Navigate to the page
                    if (isFirstPage)
                    {
                        // First page - always navigate
                        _logger.LogInformation("Navigating to first page: {Url}", step.PageUrl);
                        await page.GotoAsync(step.PageUrl, new Microsoft.Playwright.PageGotoOptions
                        {
                            WaitUntil = Microsoft.Playwright.WaitUntilState.Load,
                            Timeout = 30000
                        });
                        await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
                        await Task.Delay(1000);
                        
                        // Analyze the login page
                        var pageSteps = await AnalyzePageWithNavigationAsync(page, step.PageName, step.PageUrl, order);
                        steps.AddRange(pageSteps);
                        order += pageSteps.Count;
                        
                        // Perform login
                        if (isLoginPage)
                        {
                            _logger.LogInformation("Performing login to access subsequent pages");
                            try
                            {
                                var usernameField = await page.QuerySelectorAsync("input[type='text'], input[type='email'], input[name*='user'], input[id*='user']");
                                var passwordField = await page.QuerySelectorAsync("input[type='password']");
                                var loginButton = await page.QuerySelectorAsync("button[type='submit'], input[type='submit'], button:has-text('Log in'), button:has-text('Login'), button:has-text('Sign in')");
                                
                                if (usernameField != null && passwordField != null && loginButton != null)
                                {
                                    // Use credentials from request if provided, otherwise skip login
                                    if (!string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Password))
                                    {
                                        _logger.LogInformation("Using provided credentials for login: {Username}", request.Username);
                                        await usernameField.FillAsync(request.Username);
                                        await passwordField.FillAsync(request.Password);
                                        await loginButton.ClickAsync();
                                        await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.Load, new Microsoft.Playwright.PageWaitForLoadStateOptions { Timeout = 10000 });
                                        await Task.Delay(3000); // Wait for redirect to complete
                                        isAuthenticated = true;
                                        _logger.LogInformation("Login successful, current URL: {Url}", page.Url);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No credentials provided in request - skipping auto-login");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Auto-login failed");
                            }
                        }
                        
                        isFirstPage = false;
                    }
                    else if (isAuthenticated)
                    {
                        // Subsequent pages - navigate to target URL with authenticated session
                        _logger.LogInformation("Navigating to authenticated page: {TargetUrl}", targetUrl);
                        await page.GotoAsync(targetUrl, new Microsoft.Playwright.PageGotoOptions
                        {
                            WaitUntil = Microsoft.Playwright.WaitUntilState.Load,
                            Timeout = 30000
                        });
                        await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
                        await Task.Delay(2000);
                        
                        _logger.LogInformation("Analyzing page, current URL: {Url}", page.Url);
                        
                        // Analyze the actual page we're on
                        var pageSteps = await AnalyzePageWithNavigationAsync(page, step.PageName, targetUrl, order);
                        steps.AddRange(pageSteps);
                        order += pageSteps.Count;
                    }
                }
            }
            
            // Add logout step at the end
            _logger.LogInformation("Navigating to logout page");
            try
            {
                await page.GotoAsync("https://ehbsec.hrsa.gov/EAuthNS/internal/account/Logout", new Microsoft.Playwright.PageGotoOptions
                {
                    WaitUntil = Microsoft.Playwright.WaitUntilState.Load,
                    Timeout = 10000
                });
                await Task.Delay(1000);
                _logger.LogInformation("Logout completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Logout failed or timed out, continuing anyway");
            }
            
            // Close browser after logout
            await browser.CloseAsync();
        }

        var scenario = new
        {
            scenarioName = request.ScenarioName,
            description = request.Description,
            steps = steps
        };

        return scenario;
    }

    private async Task<List<object>> AnalyzePageWithNavigationAsync(Microsoft.Playwright.IPage page, string baseName, string baseUrl, int startOrder)
    {
        var pageSteps = new List<object>();
        var currentOrder = startOrder;
        
        // Collect ALL actions from all states of this page
        var allActions = new List<object>();
        int actionOrder = 1;
        var allElementsCount = 0;

        while (true)
        {
            // Get current page HTML
            var html = await page.ContentAsync();
            var currentUrl = page.Url;
            
            _logger.LogInformation("Analyzing page state: {Url}", currentUrl);
            var pageAnalysis = await _pageAnalyzer.AnalyzeHtmlAsync(html);
            allElementsCount += pageAnalysis.Elements.Count;
            
            // Build actions from analyzed elements for this state
            var stateActions = new List<object>();
            
            // Add actions for inputs - filter duplicates
            var seenInputs = new HashSet<string>();
            foreach (var element in pageAnalysis.Elements.Where(e => e.Type == "input"))
            {
                // Skip inputs with no useful identifiers
                if (string.IsNullOrEmpty(element.Label) && 
                    string.IsNullOrEmpty(element.Id) && 
                    string.IsNullOrEmpty(element.Name) &&
                    string.IsNullOrEmpty(element.Placeholder))
                {
                    continue;
                }
                
                var key = $"{element.Label ?? element.Id ?? element.Name}";
                if (!seenInputs.Contains(key))
                {
                    seenInputs.Add(key);
                    
                    // Determine action type based on input type
                    string actionType = "type";
                    string? actionValue = "TODO: Add value";
                    string elementLabel = element.Label ?? element.Id ?? element.Name ?? "Input";
                    
                    if (element.InputType == "checkbox" || element.InputType == "toggle")
                    {
                        actionType = "check";
                        actionValue = null; // Checkboxes don't need a value
                        
                        // Clean up long checkbox labels - extract key phrase
                        if (elementLabel.Length > 50)
                        {
                            // Try to find "I agree" or similar in the label
                            if (elementLabel.Contains("I agree", StringComparison.OrdinalIgnoreCase))
                            {
                                elementLabel = "I Agree";
                            }
                            else if (elementLabel.Contains("accept", StringComparison.OrdinalIgnoreCase))
                            {
                                elementLabel = "Accept Terms";
                            }
                            else
                            {
                                // Truncate long labels
                                elementLabel = elementLabel.Substring(0, Math.Min(50, elementLabel.Length)) + "...";
                            }
                        }
                    }
                    
                    stateActions.Add(new
                    {
                        order = actionOrder++,
                        element = elementLabel,
                        action = actionType,
                        value = actionValue,
                        selector = BuildSelector(element),
                        delayMs = 0
                    });
                }
            }
            
            // Log all buttons found
            var allButtons = pageAnalysis.Elements.Where(e => e.Type == "button").ToList();
            _logger.LogInformation("Found {Count} buttons on page: {Buttons}", 
                allButtons.Count, 
                string.Join(", ", allButtons.Select(b => b.Label ?? b.Id ?? "Unknown")));
            
            // Find navigation buttons (Login, Continue, Next, Submit, Commit, Finish, etc.)
            var navigationButtons = pageAnalysis.Elements
                .Where(e => e.Type == "button" && 
                       (e.Label?.Contains("Login", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Log in", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Sign in", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Continue", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Next", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Submit", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Commit", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Confirm", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Finish", StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
            
            _logger.LogInformation("Found {Count} navigation buttons: {Buttons}", 
                navigationButtons.Count, 
                string.Join(", ", navigationButtons.Select(b => b.Label ?? "Unknown")));

            // Check if this is a login button (don't auto-click it during analysis)
            bool isLoginButton = navigationButtons.Any() && 
                                (navigationButtons.First().Label?.Contains("Login", StringComparison.OrdinalIgnoreCase) == true ||
                                 navigationButtons.First().Label?.Contains("Log in", StringComparison.OrdinalIgnoreCase) == true ||
                                 navigationButtons.First().Label?.Contains("Sign in", StringComparison.OrdinalIgnoreCase) == true);

            // Add navigation button as action
            var hasNavigationButton = false;
            if (navigationButtons.Any())
            {
                var navButton = navigationButtons.First();
                stateActions.Add(new
                {
                    order = actionOrder++,
                    element = navButton.Label ?? "Button",
                    action = "click",
                    value = (string?)null,
                    selector = BuildSelector(navButton),
                    delayMs = 1000,
                    isNavigation = true
                });
                hasNavigationButton = true;
            }
            
            // Add state actions to the overall action list
            allActions.AddRange(stateActions);

            // If there's a navigation button AND it's NOT a login button, click it and analyze the next page
            // Login buttons should NOT be auto-clicked during analysis (they need credentials first)
            if (hasNavigationButton && navigationButtons.Any() && !isLoginButton)
            {
                try
                {
                    var navButton = navigationButtons.First();
                    var selector = BuildSelector(navButton);
                    
                    _logger.LogInformation("Clicking navigation button: {Label}", navButton.Label);
                    
                    // Get page content before clicking to detect changes
                    var htmlBefore = await page.ContentAsync();
                    var currentUrlBefore = page.Url;
                    
                    // Click and wait for navigation or page update
                    await page.ClickAsync(selector);
                    await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.Load, new Microsoft.Playwright.PageWaitForLoadStateOptions { Timeout = 10000 });
                    await Task.Delay(3000); // Wait longer for Salesforce dynamic updates
                    
                    // Get page content after clicking
                    var htmlAfter = await page.ContentAsync();
                    
                    // Check if page changed (either URL or content)
                    bool urlChanged = page.Url != currentUrlBefore;
                    bool contentChanged = htmlAfter != htmlBefore;
                    
                    if (!urlChanged && !contentChanged)
                    {
                        _logger.LogInformation("No navigation or page change occurred, stopping analysis");
                        break;
                    }
                    
                    if (urlChanged)
                    {
                        _logger.LogInformation("URL changed from {Before} to {After}", currentUrlBefore, page.Url);
                    }
                    else
                    {
                        _logger.LogInformation("Page content changed (same URL) - analyzing next state");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to click navigation button or no navigation occurred");
                    break;
                }
            }
            else
            {
                // No more navigation buttons, or it's a login button, we're done
                if (isLoginButton)
                {
                    _logger.LogInformation("Login button detected - not auto-clicking during analysis");
                }
                break;
            }
        }
        
        // Create a single step with all actions from all states
        pageSteps.Add(new
        {
            order = currentOrder,
            pageName = baseName,
            pageUrl = baseUrl,
            actualUrl = page.Url,
            actions = allActions,
            elementsFound = allElementsCount
        });

        return pageSteps;
    }

    private string BuildSelector(dynamic element)
    {
        string? id = element.Id;
        string? name = element.Name;
        string? className = element.ClassName;
        
        // Prefer name attribute as it's most reliable
        if (!string.IsNullOrEmpty(name))
            return $"[name='{name}']";
        
        // Use ID only if it's a valid CSS identifier (doesn't start with digit, no special chars)
        if (!string.IsNullOrEmpty(id) && IsValidCssId(id))
            return $"#{id}";
        
        // If ID exists but invalid, use attribute selector
        if (!string.IsNullOrEmpty(id))
            return $"[id='{id}']";
        
        if (!string.IsNullOrEmpty(className))
        {
            var classes = className.Split(' ');
            if (classes.Length > 0 && !string.IsNullOrEmpty(classes[0]))
            {
                return $".{classes[0]}";
            }
        }

        // Fallback to label if available
        string? label = element.Label;
        if (!string.IsNullOrEmpty(label))
        {
            return $"text={label}";
        }

        return "[data-testid='unknown']";
    }

    private bool IsValidCssId(string id)
    {
        // CSS IDs cannot start with a digit and should not contain colons or other special chars
        if (string.IsNullOrEmpty(id)) return false;
        if (char.IsDigit(id[0])) return false;
        if (id.Contains(':') || id.Contains('[') || id.Contains(']') || id.Contains('.')) return false;
        return true;
    }

    private string DeterminePlaywrightSelector(string elementLabel, string? providedSelector)
    {
        if (!string.IsNullOrEmpty(providedSelector))
            return providedSelector;

        // Generate a reasonable selector based on label
        var sanitized = elementLabel.ToLower().Replace(" ", "-");
        return $"[placeholder*='{elementLabel}'], [name*='{sanitized}'], [id*='{sanitized}']";
    }

    private string GenerateEndToEndScript(TestScenarioRequest request, List<PageStepResult> analyzedSteps)
    {
        var sb = new StringBuilder();

        // Generate test class header
        sb.AppendLine("using NUnit.Framework;");
        sb.AppendLine("using OpenQA.Selenium;");
        sb.AppendLine("using OpenQA.Selenium.Chrome;");
        sb.AppendLine("using OpenQA.Selenium.Support.UI;");
        sb.AppendLine();
        sb.AppendLine("namespace TestAutomationApp.Tests;");
        sb.AppendLine();
        sb.AppendLine("[TestFixture]");
        sb.AppendLine($"public class {SanitizeClassName(request.ScenarioName)}Tests");
        sb.AppendLine("{");
        sb.AppendLine("    private IWebDriver _driver;");
        sb.AppendLine("    private WebDriverWait _wait;");
        sb.AppendLine();
        
        // Setup method
        sb.AppendLine("    [SetUp]");
        sb.AppendLine("    public void Setup()");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver = new ChromeDriver();");
        sb.AppendLine("        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));");
        sb.AppendLine("        _driver.Manage().Window.Maximize();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate test method
        sb.AppendLine("    [Test]");
        sb.AppendLine($"    public void {SanitizeMethodName(request.ScenarioName)}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // {request.Description}");
        sb.AppendLine();

        // Generate steps
        foreach (var step in request.Steps.OrderBy(s => s.Order))
        {
            sb.AppendLine($"        // Step {step.Order}: {step.PageName}");
            
            // Navigate if URL provided
            if (!string.IsNullOrEmpty(step.PageUrl) && step.Order == 1)
            {
                sb.AppendLine($"        _driver.Navigate().GoToUrl(\"{step.PageUrl}\");");
                sb.AppendLine();
            }

            // Generate actions
            foreach (var action in step.Actions.OrderBy(a => a.Order))
            {
                sb.AppendLine(GenerateActionCode(action, analyzedSteps, step.Order));
            }

            // Add assertions if any
            if (step.Assertions != null && step.Assertions.Any())
            {
                sb.AppendLine();
                sb.AppendLine($"        // Assertions for {step.PageName}");
                foreach (var assertion in step.Assertions)
                {
                    sb.AppendLine(GenerateAssertionCode(assertion));
                }
            }

            // Wait for navigation if specified
            if (!string.IsNullOrEmpty(step.ExpectedNavigation))
            {
                sb.AppendLine($"        _wait.Until(d => d.Url.Contains(\"{step.ExpectedNavigation}\"));");
            }

            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // Teardown method
        sb.AppendLine("    [TearDown]");
        sb.AppendLine("    public void TearDown()");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver?.Quit();");
        sb.AppendLine("        _driver?.Dispose();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateActionCode(ElementAction action, List<PageStepResult> analyzedSteps, int stepOrder)
    {
        var sb = new StringBuilder();
        var indent = "        ";

        // Find the element from analyzed steps
        var stepResult = analyzedSteps.FirstOrDefault(s => s.Order == stepOrder);
        var element = stepResult?.AvailableElements.FirstOrDefault(e => 
            e.Label?.Equals(action.ElementLabel, StringComparison.OrdinalIgnoreCase) == true);

        // Determine selector
        string selector = action.Selector ?? DetermineSelector(element);

        switch (action.ActionType.ToLower())
        {
            case "type":
            case "sendkeys":
                sb.Append($"{indent}var {SanitizeVariableName(action.ElementLabel)} = _wait.Until(d => d.FindElement(By.{selector}));");
                sb.AppendLine();
                sb.Append($"{indent}{SanitizeVariableName(action.ElementLabel)}.Clear();");
                sb.AppendLine();
                sb.Append($"{indent}{SanitizeVariableName(action.ElementLabel)}.SendKeys(\"{action.Value}\");");
                break;

            case "click":
                sb.Append($"{indent}var {SanitizeVariableName(action.ElementLabel)} = _wait.Until(d => d.FindElement(By.{selector}));");
                sb.AppendLine();
                sb.Append($"{indent}{SanitizeVariableName(action.ElementLabel)}.Click();");
                break;

            case "select":
                sb.Append($"{indent}var {SanitizeVariableName(action.ElementLabel)} = _wait.Until(d => d.FindElement(By.{selector}));");
                sb.AppendLine();
                sb.Append($"{indent}var select{SanitizeVariableName(action.ElementLabel)} = new SelectElement({SanitizeVariableName(action.ElementLabel)});");
                sb.AppendLine();
                sb.Append($"{indent}select{SanitizeVariableName(action.ElementLabel)}.SelectByText(\"{action.Value}\");");
                break;

            default:
                sb.Append($"{indent}// TODO: Implement {action.ActionType} for {action.ElementLabel}");
                break;
        }

        // Add delay if specified
        if (action.DelayAfterMs > 0)
        {
            sb.AppendLine();
            sb.Append($"{indent}Thread.Sleep({action.DelayAfterMs});");
        }

        return sb.ToString();
    }

    private string DetermineSelector(PageElement? element)
    {
        if (element == null) return "CssSelector(\"TODO\")";

        if (!string.IsNullOrEmpty(element.Id))
            return $"Id(\"{element.Id}\")";
        
        if (!string.IsNullOrEmpty(element.Name))
            return $"Name(\"{element.Name}\")";
        
        if (!string.IsNullOrEmpty(element.ClassName))
        {
            var firstClass = element.ClassName.Split(' ').FirstOrDefault();
            return $"ClassName(\"{firstClass}\")";
        }

        return "CssSelector(\"TODO\")";
    }

    private string GenerateAssertionCode(Assertion assertion)
    {
        var indent = "        ";
        
        return assertion.Type.ToLower() switch
        {
            "url" => $"{indent}Assert.That(_driver.Url, Does.Contain(\"{assertion.Expected}\"));",
            "element" => $"{indent}Assert.That(_driver.FindElement(By.{assertion.Selector}).Displayed, Is.True);",
            "text" => $"{indent}Assert.That(_driver.FindElement(By.{assertion.Selector}).Text, Does.Contain(\"{assertion.Expected}\"));",
            _ => $"{indent}// TODO: Implement assertion type {assertion.Type}"
        };
    }

    private string SanitizeClassName(string name)
    {
        return new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
    }

    private string SanitizeMethodName(string name)
    {
        var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return char.IsDigit(sanitized[0]) ? "_" + sanitized : sanitized;
    }

    private string SanitizeVariableName(string name)
    {
        var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
        return char.ToLower(sanitized[0]) + sanitized.Substring(1);
    }
}
