using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestAutomationApp.API.Services
{
    public class BrowserAutomationService : IDisposable
    {
        private readonly ILogger<BrowserAutomationService> _logger;
        private readonly string _screenshotsPath;
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private bool _disposed = false;

        public BrowserAutomationService(ILogger<BrowserAutomationService> logger)
        {
            _logger = logger;
            _screenshotsPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
            Directory.CreateDirectory(_screenshotsPath);
        }

        public async Task<BrowserPageResult> GetPageContentAsync(string url, string username, string password)
        {
            _logger.LogInformation($"Starting browser automation for URL: {url}");
            
            try
            {
                _playwright = await Playwright.CreateAsync();
                
                // Configure browser to look more like a regular user
                _browser = await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false,  // Keep false for debugging
                    Timeout = 60000,
                    Args = new[]
                    {
                        "--disable-blink-features=AutomationControlled",
                        "--disable-infobars",
                        "--window-size=1920,1080"
                    }
                });

                // Create a new browser context with more natural settings
                var context = await _browser.NewContextAsync(new()
                {
                    ViewportSize = new() { Width = 1920, Height = 1080 },
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    JavaScriptEnabled = true,
                    BypassCSP = true,
                    IgnoreHTTPSErrors = true
                });
                
                // Disable WebDriver flag
                await context.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', { get: () => false });");
                
                var page = await context.NewPageAsync();
                page.SetDefaultNavigationTimeout(120000); // 2 minute timeout
                page.SetDefaultTimeout(60000); // 1 minute timeout for other operations

                // Enable request/response logging
                page.Request += (_, request) => _logger.LogDebug($"Request: {request.Method} {request.Url}");
                page.Response += (_, response) => _logger.LogDebug($"Response: {response.Status} {response.Url}");
                page.Console += (_, msg) => _logger.LogDebug($"Console: {msg.Type}: {msg.Text}");
                page.PageError += (_, msg) => _logger.LogError($"Page Error: {msg}");

                // Navigate to the page
                _logger.LogInformation("Navigating to: {Url}", url);
                
                var response = await page.GotoAsync(url, new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 60000,
                    Referer = "https://ehbsec.hrsa.gov/"
                });
                
                if (response == null)
                {
                    _logger.LogError("Navigation to {Url} failed - no response", url);
                    var errorScreenshot = Path.Combine(_screenshotsPath, $"nav_error_{DateTime.Now:yyyyMMddHHmmss}.png");
                    try { await page.ScreenshotAsync(new() { Path = errorScreenshot }); } catch { /* Ignore if screenshot fails */ }
                    
                    return new BrowserPageResult
                    {
                        Success = false,
                        Title = "Navigation Failed",
                        Content = "Failed to load the page - no response from server",
                        Message = "Navigation failed - no response from server",
                        ScreenshotPath = errorScreenshot
                    };
                }
                
                _logger.LogInformation("Navigation complete. Status: {Status} {StatusText}", response.Status, response.StatusText);
                
                // Wait a moment for any JavaScript to execute
                await page.WaitForTimeoutAsync(2000);
                
                // Check if we're on a login page
                var isLoginPage = await page.IsVisibleAsync("input[type='password'], input#Password, input#password");
                
                if (isLoginPage && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _logger.LogInformation("Login page detected, attempting to log in...");
                    
                    // Try different selectors for username field
                    var usernameField = await page.QuerySelectorAsync("input#UserName, input#username, input[type='email'], input[name*='user']");
                    var passwordField = await page.QuerySelectorAsync("input#Password, input#password, input[type='password']");
                    var loginButton = await page.QuerySelectorAsync("button#Login, input[type='submit'], button:has-text('Log In'), button:has-text('Sign In')");
                    
                    if (usernameField != null && passwordField != null)
                    {
                        try 
                        {
                            _logger.LogInformation("Filling login form...");
                            
                            // Clear and type username
                            await usernameField.ClickAsync();
                            await page.Keyboard.PressAsync("Control+A");
                            await page.Keyboard.PressAsync("Backspace");
                            await usernameField.TypeAsync(username);
                            
                            // Clear and type password
                            await passwordField.ClickAsync();
                            await page.Keyboard.PressAsync("Control+A");
                            await page.Keyboard.PressAsync("Backspace");
                            await passwordField.TypeAsync(password);
                            
                            _logger.LogInformation("Submitting login form...");
                            
                            // Try to click the login button if found
                            if (loginButton != null)
                            {
                                await loginButton.ClickAsync();
                            }
                            else
                            {
                                // If no button found, try submitting the form by pressing Enter
                                await page.Keyboard.PressAsync("Enter");
                            }
                            
                            // Wait for navigation after login
                            await page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                            _logger.LogInformation("Login form submitted, waiting for navigation...");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during login form submission");
                            // Continue even if login fails, we'll check the result below
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not find all required login fields");
                    }
                }
                
                // Take a screenshot of the final state
                var screenshotPath = Path.Combine(_screenshotsPath, $"result_{DateTime.Now:yyyyMMddHHmmss}.png");
                await page.ScreenshotAsync(new() { Path = screenshotPath });
                
                // Get the page content
                var title = await page.TitleAsync();
                var content = await page.ContentAsync();
                
                return new BrowserPageResult
                {
                    Success = true,
                    Title = title,
                    Content = content,
                    ScreenshotPath = screenshotPath,
                    Message = "Page loaded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPageContentAsync");
                return new BrowserPageResult
                {
                    Success = false,
                    Title = "Error",
                    Content = ex.Message,
                    Message = $"Error: {ex.Message}",
                    ScreenshotPath = null
                };
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _browser?.CloseAsync().GetAwaiter().GetResult();
                    _browser?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    _playwright?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    public class BrowserPageResult
    {
        public bool Success { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public string? ScreenshotPath { get; set; }
        public string? Message { get; set; }
    }
}
