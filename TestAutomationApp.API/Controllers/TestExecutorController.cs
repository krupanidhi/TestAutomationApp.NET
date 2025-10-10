using Microsoft.AspNetCore.Mvc;
using TestAutomationApp.API.Services;

namespace TestAutomationApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestExecutorController : ControllerBase
{
    private readonly ITestExecutorService _executorService;
    private readonly ILogger<TestExecutorController> _logger;

    public TestExecutorController(
        ITestExecutorService executorService,
        ILogger<TestExecutorController> logger)
    {
        _executorService = executorService;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<TestExecutionResult>> ExecuteTest([FromBody] ExecuteTestRequest request)
    {
        try
        {
            _logger.LogInformation("Executing test: {TestJson}", request.TestJson?.Substring(0, Math.Min(100, request.TestJson?.Length ?? 0)));
            
            var result = await _executorService.ExecuteTestAsync(request.TestJson ?? "");
            
            _logger.LogInformation("Returning result: Status={Status}, Steps={StepCount}, Actions={ActionCount}", 
                result.Status, 
                result.StepResults?.Count ?? 0,
                result.StepResults?.Sum(s => s.ActionResults?.Count ?? 0) ?? 0);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing test");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ExecuteTestRequest
{
    public string? TestJson { get; set; }
}
