using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAutomationApp.API.Data;
using TestAutomationApp.API.Services;
using TestAutomationApp.Shared.DTOs;
using TestAutomationApp.Shared.Models;

namespace TestAutomationApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestScriptsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITestGeneratorService _testGenerator;
    private readonly ILogger<TestScriptsController> _logger;

    public TestScriptsController(
        ApplicationDbContext context,
        ITestGeneratorService testGenerator,
        ILogger<TestScriptsController> logger)
    {
        _context = context;
        _testGenerator = testGenerator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TestScript>>> GetTestScripts()
    {
        try
        {
            var scripts = await _context.TestScripts
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return Ok(scripts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test scripts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TestScript>> GetTestScript(int id)
    {
        try
        {
            var script = await _context.TestScripts.FindAsync(id);
            if (script == null)
            {
                return NotFound();
            }
            return Ok(script);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test script {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateTestResponse>> GenerateTest(GenerateTestRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Generating test script for framework: {Framework}", request.TestFramework);

            var generatedScript = await _testGenerator.GenerateTestScriptAsync(
                request.UiDescription,
                request.TestFramework);

            var testScript = new TestScript
            {
                UiDescription = request.UiDescription,
                TestFramework = request.TestFramework,
                GeneratedScript = generatedScript,
                CreatedAt = DateTime.UtcNow
            };

            _context.TestScripts.Add(testScript);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Test script generated successfully: {Id}", testScript.Id);

            return Ok(new GenerateTestResponse
            {
                Id = testScript.Id,
                Script = generatedScript,
                Message = "Test script generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test script");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTestScript(int id)
    {
        var script = await _context.TestScripts.FindAsync(id);
        if (script == null)
        {
            return NotFound();
        }

        _context.TestScripts.Remove(script);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
