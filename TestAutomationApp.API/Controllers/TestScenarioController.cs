using Microsoft.AspNetCore.Mvc;
using TestAutomationApp.API.Services;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestScenarioController : ControllerBase
{
    private readonly ITestScenarioService _scenarioService;
    private readonly ILogger<TestScenarioController> _logger;

    public TestScenarioController(
        ITestScenarioService scenarioService,
        ILogger<TestScenarioController> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    [HttpPost("analyze-and-generate")]
    public async Task<ActionResult<TestScenarioResponse>> AnalyzeAndGenerate([FromBody] TestScenarioRequest request)
    {
        try
        {
            _logger.LogInformation("Generating end-to-end test scenario: {ScenarioName}", request.ScenarioName);
            
            var result = await _scenarioService.AnalyzeAndBuildScenarioAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test scenario");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("generate")]
    public async Task<ActionResult<TestScenarioResponse>> Generate([FromBody] TestScenarioRequest request)
    {
        try
        {
            _logger.LogInformation("Generating test scenario from configuration: {ScenarioName}", request.ScenarioName);
            
            var result = await _scenarioService.GenerateScenarioTestAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test scenario");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("generate-json")]
    public async Task<ActionResult<object>> GenerateJson([FromBody] TestScenarioRequest request)
    {
        try
        {
            _logger.LogInformation("Generating Playwright JSON scenario: {ScenarioName}", request.ScenarioName);
            
            var result = await _scenarioService.GeneratePlaywrightJsonAsync(request);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON scenario");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
