using Microsoft.AspNetCore.Mvc;
using TestAutomationApp.API.Services;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestDataController : ControllerBase
{
    private readonly ITestDataService _testDataService;
    private readonly ILogger<TestDataController> _logger;

    public TestDataController(
        ITestDataService testDataService,
        ILogger<TestDataController> logger)
    {
        _testDataService = testDataService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<TestDataResponse>> CreateTestDataSet([FromBody] TestDataRequest request)
    {
        try
        {
            _logger.LogInformation("Creating test data set: {Name}", request.Name);
            var result = await _testDataService.CreateTestDataSetAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test data set");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<TestDataListResponse>> GetAllTestDataSets()
    {
        try
        {
            var result = await _testDataService.GetAllTestDataSetsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test data sets");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TestDataResponse>> GetTestDataSet(string id)
    {
        try
        {
            var result = await _testDataService.GetTestDataSetAsync(id);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test data set");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TestDataResponse>> UpdateTestDataSet(string id, [FromBody] TestDataRequest request)
    {
        try
        {
            _logger.LogInformation("Updating test data set: {Id}", id);
            var result = await _testDataService.UpdateTestDataSetAsync(id, request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test data set");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTestDataSet(string id)
    {
        try
        {
            _logger.LogInformation("Deleting test data set: {Id}", id);
            var result = await _testDataService.DeleteTestDataSetAsync(id);
            
            if (result)
            {
                return Ok(new { message = "Test data set deleted successfully" });
            }
            
            return NotFound(new { message = "Test data set not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting test data set");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
