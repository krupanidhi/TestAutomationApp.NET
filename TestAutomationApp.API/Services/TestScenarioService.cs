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
                    _logger.LogInformation("Navigating to page {Order}: {Url}", order, step.PageUrl);
                    
                    // Check if this is a login page
                    bool isLoginPage = step.PageUrl.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                                      step.PageUrl.Contains("signin", StringComparison.OrdinalIgnoreCase);
                    
                    // Navigate to the URL (maintains session)
                    await page.GotoAsync(step.PageUrl, new Microsoft.Playwright.PageGotoOptions
                    {
                        WaitUntil = Microsoft.Playwright.WaitUntilState.Load,
                        Timeout = 30000
                    });

                    // Wait a bit for dynamic content
                    await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
                    await Task.Delay(1000);

                    // FIRST: Analyze the page to capture form elements
                    var pageSteps = await AnalyzePageWithNavigationAsync(page, step.PageName, step.PageUrl, order);
                    steps.AddRange(pageSteps);
                    order += pageSteps.Count;

                    // THEN: If this is a login page, perform the actual login for subsequent pages
                    if (isFirstPage && isLoginPage && !isAuthenticated)
                    {
                        _logger.LogInformation("Performing login to access subsequent pages");
                        try
                        {
                            // Navigate back to login page if we got redirected
                            if (!page.Url.Contains("login", StringComparison.OrdinalIgnoreCase) && 
                                !page.Url.Contains("signin", StringComparison.OrdinalIgnoreCase))
                            {
                                await page.GotoAsync(step.PageUrl);
                                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.Load);
                                await Task.Delay(1000);
                            }
                            
                            // Try to login using configured credentials
                            var usernameField = await page.QuerySelectorAsync("input[type='text'], input[type='email'], input[name*='user'], input[id*='user']");
                            var passwordField = await page.QuerySelectorAsync("input[type='password']");
                            var loginButton = await page.QuerySelectorAsync("button[type='submit'], input[type='submit'], button:has-text('Log in'), button:has-text('Login'), button:has-text('Sign in')");
                            
                            if (usernameField != null && passwordField != null && loginButton != null)
                            {
                                await usernameField.FillAsync("asrilam"); // From config
                                await passwordField.FillAsync("3ePkpVBMzya4aICkLRyB"); // From config
                                await loginButton.ClickAsync();
                                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.Load, new Microsoft.Playwright.PageWaitForLoadStateOptions { Timeout = 10000 });
                                await Task.Delay(2000);
                                isAuthenticated = true;
                                _logger.LogInformation("Login successful, current URL: {Url}", page.Url);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Auto-login failed, continuing with analysis");
                        }
                    }
                    
                    isFirstPage = false;
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
        var pageNumber = 1;

        while (true)
        {
            // Get current page HTML
            var html = await page.ContentAsync();
            var currentUrl = page.Url;
            
            _logger.LogInformation("Analyzing page {Order}: {Url}", currentOrder, currentUrl);
            var pageAnalysis = await _pageAnalyzer.AnalyzeHtmlAsync(html);
            
            // Build actions from analyzed elements
            var actions = new List<object>();
            int actionOrder = 1;
            
            // Add actions for inputs (type actions) - filter duplicates
            var seenInputs = new HashSet<string>();
            foreach (var element in pageAnalysis.Elements.Where(e => e.Type == "input"))
            {
                var key = $"{element.Label ?? element.Id ?? element.Name}";
                if (!seenInputs.Contains(key))
                {
                    seenInputs.Add(key);
                    actions.Add(new
                    {
                        order = actionOrder++,
                        element = element.Label ?? element.Id ?? element.Name ?? "Input",
                        action = "type",
                        value = "TODO: Add value",
                        selector = BuildSelector(element),
                        delayMs = 0
                    });
                }
            }
            
            // Find navigation buttons (Login, Continue, Next, Submit, Commit, etc.)
            var navigationButtons = pageAnalysis.Elements
                .Where(e => e.Type == "button" && 
                       (e.Label?.Contains("Login", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Log in", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Sign in", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Continue", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Next", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Submit", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Commit", StringComparison.OrdinalIgnoreCase) == true ||
                        e.Label?.Contains("Confirm", StringComparison.OrdinalIgnoreCase) == true))
                .ToList();

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
                actions.Add(new
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

            // Add current page to steps
            pageSteps.Add(new
            {
                order = currentOrder++,
                pageName = pageNumber == 1 ? baseName : $"{baseName}_Step{pageNumber}",
                pageUrl = pageNumber == 1 ? baseUrl : currentUrl,
                actualUrl = currentUrl,
                actions = actions,
                elementsFound = pageAnalysis.Elements.Count
            });

            // If there's a navigation button AND it's NOT a login button, click it and analyze the next page
            // Login buttons should NOT be auto-clicked during analysis (they need credentials first)
            if (hasNavigationButton && navigationButtons.Any() && !isLoginButton)
            {
                try
                {
                    var navButton = navigationButtons.First();
                    var selector = BuildSelector(navButton);
                    
                    _logger.LogInformation("Clicking navigation button: {Label}", navButton.Label);
                    
                    // Click and wait for navigation
                    var currentUrlBefore = page.Url;
                    await page.ClickAsync(selector);
                    await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.Load, new Microsoft.Playwright.PageWaitForLoadStateOptions { Timeout = 10000 });
                    await Task.Delay(1000);
                    
                    // Check if we actually navigated to a different page
                    if (page.Url == currentUrlBefore)
                    {
                        _logger.LogInformation("No navigation occurred, stopping analysis");
                        break;
                    }
                    
                    pageNumber++;
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

        return pageSteps;
    }

    private string BuildSelector(dynamic element)
    {
        if (!string.IsNullOrEmpty(element.Id))
            return $"#{element.Id}";
        
        if (!string.IsNullOrEmpty(element.Name))
            return $"[name='{element.Name}']";
        
        if (!string.IsNullOrEmpty(element.ClassName))
        {
            var firstClass = element.ClassName.Split(' ').FirstOrDefault();
            return $".{firstClass}";
        }

        return "[data-testid='unknown']";
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
