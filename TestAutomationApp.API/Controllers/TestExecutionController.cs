using Microsoft.AspNetCore.Mvc;
using TestAutomationApp.API.Services;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestExecutionController : ControllerBase
{
    private readonly IPlaywrightExecutorService _executorService;
    private readonly ILogger<TestExecutionController> _logger;

    public TestExecutionController(IPlaywrightExecutorService executorService, ILogger<TestExecutionController> logger)
    {
        _executorService = executorService;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ExecuteTestResponse>> ExecuteTest([FromBody] ExecuteTestRequest request)
    {
        try
        {
            var result = await _executorService.ExecuteJsonTestAsync(
                request.JsonScript,
                request.Headless,
                request.CaptureScreenshots,
                request.CaptureVideo
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing test");
            return StatusCode(500, new ExecuteTestResponse
            {
                Success = false,
                Message = $"Execution error: {ex.Message}"
            });
        }
    }
}