using Microsoft.Playwright;
using System.Diagnostics;
using System.Text.Json;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Services;

public interface IPlaywrightExecutorService
{
    Task<ExecuteTestResponse> ExecuteJsonTestAsync(string jsonScript, bool headless, bool captureScreenshots, bool captureVideo);
}

public class PlaywrightExecutorService : IPlaywrightExecutorService
{
    private readonly ILogger<PlaywrightExecutorService> _logger;
    private readonly string _screenshotDir;
    private readonly string _videoDir;

    public PlaywrightExecutorService(ILogger<PlaywrightExecutorService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _screenshotDir = Path.Combine(env.ContentRootPath, "wwwroot", "test-screenshots");
        _videoDir = Path.Combine(env.ContentRootPath, "wwwroot", "test-videos");

        Directory.CreateDirectory(_screenshotDir);
        Directory.CreateDirectory(_videoDir);
    }

    public async Task<ExecuteTestResponse> ExecuteJsonTestAsync(string jsonScript, bool headless, bool captureScreenshots, bool captureVideo)
    {
        var response = new ExecuteTestResponse();
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            var testData = JsonSerializer.Deserialize<JsonElement>(jsonScript);
            var testCases = testData.GetProperty("testCases").EnumerateArray().ToList();

            if (!testCases.Any())
            {
                response.Success = false;
                response.Message = "No test cases found in JSON";
                return response;
            }

            // Install Playwright if needed
            await EnsurePlaywrightInstalledAsync();

            // Execute first test case
            var testCase = testCases[0];
            await ExecuteTestCaseAsync(testCase, response, headless, captureScreenshots, captureVideo);

            totalStopwatch.Stop();
            response.DurationMs = totalStopwatch.ElapsedMilliseconds;
            response.Success = response.StepResults.All(s => s.Success);
            response.Message = response.Success
                ? $"Test executed successfully! {response.StepResults.Count} steps completed in {response.DurationMs}ms"
                : $"Test failed. {response.StepResults.Count(s => !s.Success)} step(s) failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Playwright test");
            response.Success = false;
            response.Message = $"Execution error: {ex.Message}";
        }

        return response;
    }

    private async Task ExecuteTestCaseAsync(JsonElement testCase, ExecuteTestResponse response, bool headless, bool captureScreenshots, bool captureVideo)
    {
        var playwright = await Playwright.CreateAsync();
        IBrowser? browser = null;
        IPage? page = null;

        try
        {
            // Launch browser
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = headless,
                SlowMo = 100 // Slow down by 100ms for visibility
            };

            browser = await playwright.Chromium.LaunchAsync(launchOptions);

            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                RecordVideoDir = captureVideo ? _videoDir : null
            };

            var context = await browser.NewContextAsync(contextOptions);
            page = await context.NewPageAsync();

            // Get steps
            var steps = testCase.GetProperty("steps").EnumerateArray().ToList();

            foreach (var step in steps)
            {
                var stepResult = await ExecuteStepAsync(page, step, captureScreenshots);
                response.StepResults.Add(stepResult);

                if (!stepResult.Success)
                {
                    // Continue execution even on failure to capture all results
                    _logger.LogWarning($"Step {stepResult.Order} failed: {stepResult.Error}");
                }
            }

            // Capture final screenshot
            if (captureScreenshots)
            {
                var finalScreenshot = await CaptureScreenshotAsync(page, "final-state");
                response.Screenshots.Add(finalScreenshot);
            }

            // Get video path if recorded
            if (captureVideo)
            {
                await page.CloseAsync();
                var video = page.Video;
                if (video != null)
                {
                    response.VideoPath = await video.PathAsync();
                }
            }
        }
        finally
        {
            if (page != null) await page.CloseAsync();
            if (browser != null) await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    private async Task<StepResult> ExecuteStepAsync(IPage page, JsonElement step, bool captureScreenshots)
    {
        var stepResult = new StepResult
        {
            Order = step.GetProperty("order").GetInt32(),
            Description = step.GetProperty("description").GetString() ?? ""
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var action = step.GetProperty("action").GetString() ?? "";
            var timeout = step.TryGetProperty("options", out var options) && options.TryGetProperty("timeout", out var timeoutProp)
                ? timeoutProp.GetInt32()
                : 30000;

            switch (action.ToLower())
            {
                case "navigate":
                    var url = step.GetProperty("url").GetString();
                    await page.GotoAsync(url, new PageGotoOptions { Timeout = timeout });
                    break;

                case "fill":
                case "type":
                    var fillSelector = step.GetProperty("selector").GetString() ?? "";
                    var fillValue = step.GetProperty("value").GetString() ?? "";
                    await page.FillAsync(fillSelector, fillValue, new PageFillOptions { Timeout = timeout });
                    break;

                case "click":
                    var clickSelector = step.GetProperty("selector").GetString() ?? "";
                    await page.ClickAsync(clickSelector, new PageClickOptions { Timeout = timeout });
                    break;

                case "selectoption":
                    var selectSelector = step.GetProperty("selector").GetString() ?? "";
                    var selectValue = step.GetProperty("value").GetString() ?? "";
                    await page.SelectOptionAsync(selectSelector, selectValue, new PageSelectOptionOptions { Timeout = timeout });
                    break;

                case "check":
                    var checkSelector = step.GetProperty("selector").GetString() ?? "";
                    await page.CheckAsync(checkSelector, new PageCheckOptions { Timeout = timeout });
                    break;

                case "uncheck":
                    var uncheckSelector = step.GetProperty("selector").GetString() ?? "";
                    await page.UncheckAsync(uncheckSelector, new PageUncheckOptions { Timeout = timeout });
                    break;

                case "hover":
                    var hoverSelector = step.GetProperty("selector").GetString() ?? "";
                    await page.HoverAsync(hoverSelector, new PageHoverOptions { Timeout = timeout });
                    break;

                case "waitforselector":
                    var waitSelector = step.GetProperty("selector").GetString() ?? "";
                    await page.WaitForSelectorAsync(waitSelector, new PageWaitForSelectorOptions { Timeout = timeout });
                    break;

                case "waitforloadstate":
                    var state = step.GetProperty("value").GetString() ?? "domcontentloaded";
                    await page.WaitForLoadStateAsync(state switch
                    {
                        "networkidle" => LoadState.NetworkIdle,
                        "load" => LoadState.Load,
                        _ => LoadState.DOMContentLoaded
                    });
                    break;

                case "wait":
                    var waitMs = int.Parse(step.GetProperty("value").GetString() ?? "1000");
                    await Task.Delay(waitMs);
                    break;

                case "screenshot":
                    var screenshotPath = await CaptureScreenshotAsync(page, $"step-{stepResult.Order}");
                    stepResult.Screenshot = screenshotPath;
                    break;

                case "assertvisible":
                    var assertSelector = step.GetProperty("selector").GetString() ?? "";
                    var isVisible = await page.Locator(assertSelector).IsVisibleAsync();
                    if (!isVisible)
                        throw new Exception($"Element {assertSelector} is not visible");
                    break;

                case "asserttext":
                    var textSelector = step.GetProperty("selector").GetString() ?? "";
                    var expectedText = step.GetProperty("value").GetString() ?? "";
                    var actualText = await page.Locator(textSelector).TextContentAsync();
                    if (!actualText?.Contains(expectedText, StringComparison.OrdinalIgnoreCase) == true)
                        throw new Exception($"Expected text '{expectedText}' not found. Actual: '{actualText}'");
                    break;

                case "scrollintoview":
                    var scrollSelector = step.GetProperty("selector").GetString() ?? "";
                    await page.Locator(scrollSelector).ScrollIntoViewIfNeededAsync();
                    break;

                default:
                    _logger.LogWarning($"Unknown action: {action}");
                    break;
            }

            // Capture screenshot after step if enabled
            if (captureScreenshots && action != "screenshot")
            {
                stepResult.Screenshot = await CaptureScreenshotAsync(page, $"step-{stepResult.Order}");
            }

            stepResult.Success = true;
        }
        catch (Exception ex)
        {
            stepResult.Success = false;
            stepResult.Error = ex.Message;
            _logger.LogError(ex, $"Step {stepResult.Order} failed");

            // Capture failure screenshot
            if (captureScreenshots)
            {
                try
                {
                    stepResult.Screenshot = await CaptureScreenshotAsync(page, $"step-{stepResult.Order}-FAILED");
                }
                catch { }
            }
        }

        stopwatch.Stop();
        stepResult.DurationMs = stopwatch.ElapsedMilliseconds;
        return stepResult;
    }

    private async Task<string> CaptureScreenshotAsync(IPage page, string name)
    {
        var fileName = $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.png";
        var filePath = Path.Combine(_screenshotDir, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = true
        });

        return $"/test-screenshots/{fileName}";
    }

    private async Task EnsurePlaywrightInstalledAsync()
    {
        try
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
            if (exitCode != 0)
            {
                _logger.LogWarning("Playwright install returned non-zero exit code");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring Playwright is installed");
        }
    }
}