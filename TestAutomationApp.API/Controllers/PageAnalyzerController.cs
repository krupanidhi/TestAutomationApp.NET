using Microsoft.AspNetCore.Mvc;
using TestAutomationApp.API.Services;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PageAnalyzerController : ControllerBase
{
    private readonly IPageAnalyzerService _pageAnalyzer;
    private readonly ILogger<PageAnalyzerController> _logger;

    public PageAnalyzerController(
        IPageAnalyzerService pageAnalyzer,
        ILogger<PageAnalyzerController> logger)
    {
        _pageAnalyzer = pageAnalyzer;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzePageResponse>> AnalyzePage([FromBody] AnalyzePageRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing page using method: {Method}", request.Method);
            
            // For URL method, use the version that supports authentication
            if (request.Method == AnalysisMethod.Url && !string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Password))
            {
                _logger.LogInformation("Using authenticated request for URL: {Url}", request.Url);
                var result = await _pageAnalyzer.AnalyzeUrlAsync(request.Url!, request.Username, request.Password);
                return Ok(result);
            }
            
            // For all other cases, use the standard method
            var standardResult = await _pageAnalyzer.AnalyzePageAsync(request);
            return Ok(standardResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing page");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("analyze-html")]
    public async Task<ActionResult<AnalyzePageResponse>> AnalyzeHtml([FromBody] string html)
    {
        try
        {
            var result = await _pageAnalyzer.AnalyzeHtmlAsync(html);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing HTML");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("analyze-url")]
    public async Task<ActionResult<AnalyzePageResponse>> AnalyzeUrl([FromBody] string url)
    {
        try
        {
            var result = await _pageAnalyzer.AnalyzeUrlAsync(url);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing URL");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("analyze-screenshot")]
    public async Task<ActionResult<AnalyzePageResponse>> AnalyzeScreenshot([FromBody] string base64Image)
    {
        try
        {
            var result = await _pageAnalyzer.AnalyzeScreenshotAsync(base64Image);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing screenshot");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
