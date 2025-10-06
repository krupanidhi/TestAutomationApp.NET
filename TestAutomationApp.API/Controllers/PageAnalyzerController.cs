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
            var result = await _pageAnalyzer.AnalyzePageAsync(request);
            return Ok(result);
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
