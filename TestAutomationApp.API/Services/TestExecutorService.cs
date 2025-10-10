using Microsoft.Playwright;
using System.Text.Json;

namespace TestAutomationApp.API.Services;

public interface ITestExecutorService
{
    Task<TestExecutionResult> ExecuteTestAsync(string testJson);
    IAsyncEnumerable<TestExecutionEvent> ExecuteTestWithLiveUpdatesAsync(string testJson);
}

public class TestExecutorService : ITestExecutorService
{
    private readonly ILogger<TestExecutorService> _logger;
    private readonly ITestDataService _testDataService;

    public TestExecutorService(
        ILogger<TestExecutorService> logger,
        ITestDataService testDataService)
    {
        _logger = logger;
        _testDataService = testDataService;
    }

    public async Task<TestExecutionResult> ExecuteTestAsync(string testJson)
    {
        var result = new TestExecutionResult
        {
            StartTime = DateTime.UtcNow,
            Status = "Running"
        };

        try
        {
            _logger.LogInformation("Starting test execution...");
            _logger.LogInformation("Test JSON length: {Length}", testJson?.Length ?? 0);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var testScenario = JsonSerializer.Deserialize<TestScenario>(testJson, options);
            if (testScenario == null)
            {
                _logger.LogError("Failed to deserialize test JSON");
                result.Status = "Failed";
                result.ErrorMessage = "Invalid test JSON - deserialization returned null";
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger.LogInformation("Test scenario loaded: {Name}, Steps: {Count}", testScenario.ScenarioName, testScenario.Steps?.Count ?? 0);
            result.ScenarioName = testScenario.ScenarioName;

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            foreach (var step in testScenario.Steps.OrderBy(s => s.Order))
            {
                var stepResult = new StepExecutionResult
                {
                    StepOrder = step.Order,
                    PageName = step.PageName,
                    StartTime = DateTime.UtcNow
                };

                try
                {
                    _logger.LogInformation("Executing step {Order}: {PageName}", step.Order, step.PageName);

                    // Navigate to page if URL is provided
                    if (!string.IsNullOrEmpty(step.PageUrl))
                    {
                        await page.GotoAsync(step.PageUrl, new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.Load,
                            Timeout = 30000
                        });
                        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    }

                    // Execute actions
                    var orderedActions = step.Actions.OrderBy(a => a.Order).ToList();
                    var lastNavigationActionIndex = -1;
                    
                    // Find the last navigation action
                    for (int i = orderedActions.Count - 1; i >= 0; i--)
                    {
                        if (orderedActions[i].IsNavigation == true)
                        {
                            lastNavigationActionIndex = i;
                            break;
                        }
                    }

                    for (int i = 0; i < orderedActions.Count; i++)
                    {
                        var action = orderedActions[i];
                        var actionResult = new ActionExecutionResult
                        {
                            ActionOrder = action.Order,
                            Element = action.Element,
                            ActionType = action.Action,
                            StartTime = DateTime.UtcNow
                        };

                        try
                        {
                            _logger.LogInformation("Executing action {Order}: {Action} on {Element}", 
                                action.Order, action.Action, action.Element);

                            // Take screenshot BEFORE the last navigation action
                            if (i == lastNavigationActionIndex && lastNavigationActionIndex >= 0)
                            {
                                try
                                {
                                    _logger.LogInformation("Taking screenshot before navigation action");
                                    await Task.Delay(500); // Brief wait for page to be stable
                                    var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                                    {
                                        FullPage = false
                                    });
                                    stepResult.Screenshot = Convert.ToBase64String(screenshotBytes);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to capture screenshot before navigation");
                                }
                            }

                            switch (action.Action.ToLower())
                            {
                                case "type":
                                    if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                                    {
                                        await page.FillAsync(action.Selector, action.Value, new PageFillOptions { Timeout = 5000 });
                                        actionResult.Status = "Passed";
                                    }
                                    break;

                                case "click":
                                    if (!string.IsNullOrEmpty(action.Selector))
                                    {
                                        await page.ClickAsync(action.Selector, new PageClickOptions { Timeout = 5000 });
                                        
                                        // If it's a navigation action, wait for page load
                                        if (action.IsNavigation == true)
                                        {
                                            await page.WaitForLoadStateAsync(LoadState.Load, new PageWaitForLoadStateOptions { Timeout = 10000 });
                                            await Task.Delay(2000); // Extra wait for dynamic content
                                        }
                                        
                                        actionResult.Status = "Passed";
                                    }
                                    break;

                                default:
                                    actionResult.Status = "Skipped";
                                    actionResult.ErrorMessage = $"Unknown action type: {action.Action}";
                                    break;
                            }

                            // Delay if specified
                            if (action.DelayMs > 0)
                            {
                                await Task.Delay(action.DelayMs);
                            }
                        }
                        catch (Exception ex)
                        {
                            actionResult.Status = "Failed";
                            actionResult.ErrorMessage = ex.Message;
                            _logger.LogError(ex, "Action failed: {Action} on {Element}", action.Action, action.Element);
                        }

                        actionResult.EndTime = DateTime.UtcNow;
                        stepResult.ActionResults.Add(actionResult);
                    }

                    // If no navigation action, take screenshot at the end
                    if (lastNavigationActionIndex < 0 && string.IsNullOrEmpty(stepResult.Screenshot))
                    {
                        try
                        {
                            await Task.Delay(1000);
                            var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                            {
                                FullPage = false
                            });
                            stepResult.Screenshot = Convert.ToBase64String(screenshotBytes);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to capture screenshot");
                        }
                    }

                    stepResult.Status = stepResult.ActionResults.All(a => a.Status == "Passed") ? "Passed" : "Failed";
                }
                catch (Exception ex)
                {
                    stepResult.Status = "Failed";
                    stepResult.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Step failed: {PageName}", step.PageName);
                }

                stepResult.EndTime = DateTime.UtcNow;
                result.StepResults.Add(stepResult);

                // Stop execution if step failed
                if (stepResult.Status == "Failed")
                {
                    break;
                }
            }

            // Always logout before closing browser to end the session
            try
            {
                _logger.LogInformation("Logging out to end session...");
                await page.GotoAsync("https://ehbsec.hrsa.gov/EAuthNS/internal/account/Logout", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 10000
                });
                await Task.Delay(2000); // Wait for logout to complete
                _logger.LogInformation("Logout completed successfully");
            }
            catch (Exception logoutEx)
            {
                _logger.LogWarning(logoutEx, "Failed to logout, but continuing to close browser");
            }

            await browser.CloseAsync();

            result.Status = result.StepResults.All(s => s.Status == "Passed") ? "Passed" : "Failed";
            _logger.LogInformation("Test execution completed: {Status}, Steps: {Count}", result.Status, result.StepResults.Count);
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Test execution failed with exception");
        }

        result.EndTime = DateTime.UtcNow;
        _logger.LogInformation("Returning result: Status={Status}, Duration={Duration}s", result.Status, result.DurationSeconds);
        return result;
    }

    public async IAsyncEnumerable<TestExecutionEvent> ExecuteTestWithLiveUpdatesAsync(string testJson)
    {
        yield return new TestExecutionEvent
        {
            Type = "started",
            Message = "Test execution started",
            Timestamp = DateTime.UtcNow
        };

        var result = await ExecuteTestAsync(testJson);

        foreach (var step in result.StepResults)
        {
            yield return new TestExecutionEvent
            {
                Type = "step_completed",
                Message = $"Step {step.StepOrder}: {step.PageName} - {step.Status}",
                Data = step,
                Timestamp = DateTime.UtcNow
            };
        }

        yield return new TestExecutionEvent
        {
            Type = "completed",
            Message = $"Test execution completed - {result.Status}",
            Data = result,
            Timestamp = DateTime.UtcNow
        };
    }
}

// DTOs
public class TestScenario
{
    public string ScenarioName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<TestStep> Steps { get; set; } = new();
}

public class TestStep
{
    public int Order { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public List<TestAction> Actions { get; set; } = new();
}

public class TestAction
{
    public int Order { get; set; }
    public string Element { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Selector { get; set; }
    public int DelayMs { get; set; }
    public bool? IsNavigation { get; set; }
}

public class TestExecutionResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Running, Passed, Failed
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public List<StepExecutionResult> StepResults { get; set; } = new();
    
    public double DurationSeconds => EndTime.HasValue 
        ? (EndTime.Value - StartTime).TotalSeconds 
        : (DateTime.UtcNow - StartTime).TotalSeconds;
}

public class StepExecutionResult
{
    public int StepOrder { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Screenshot { get; set; } // Base64 encoded
    public List<ActionExecutionResult> ActionResults { get; set; } = new();
}

public class ActionExecutionResult
{
    public int ActionOrder { get; set; }
    public string Element { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TestExecutionEvent
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; }
}
