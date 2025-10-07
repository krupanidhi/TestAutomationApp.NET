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
    [HttpPost("generate-json")]
    public async Task<ActionResult<GenerateJsonTestResponse>> GenerateJsonFromElements([FromBody] GenerateJsonTestRequest request)
    {
        try
        {
            var jsonScript = GeneratePlaywrightJsonFromElements(request);

            return Ok(new GenerateJsonTestResponse
            {
                JsonScript = jsonScript,
                Message = "JSON test script generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON test script");
            return StatusCode(500, new GenerateJsonTestResponse
            {
                Message = $"Error: {ex.Message}"
            });
        }
    }

    private string GeneratePlaywrightJsonFromElements(GenerateJsonTestRequest request)
    {
        var steps = new List<object>();
        int order = 1;

        // Navigate step
        steps.Add(new
        {
            action = "navigate",
            url = request.Url,
            description = $"Navigate to {request.Url}",
            order = order++,
            options = new { waitUntil = "networkidle" }
        });

        // Generate steps from selected elements
        foreach (var selected in request.SelectedElements.OrderBy(e => e.Order))
        {
            var selector = GetBestSelectorForElement(selected.Element);

            var step = new Dictionary<string, object>
            {
                ["action"] = selected.ActionType,
                ["selector"] = selector,
                ["description"] = selected.ActionDescription,
                ["order"] = order++
            };

            if (selected.ActionType == "fill" && !string.IsNullOrEmpty(selected.TestValue))
            {
                step["value"] = selected.TestValue;
            }

            if (selected.ActionType == "selectOption")
            {
                step["value"] = selected.TestValue ?? "option1";
            }

            step["options"] = new { timeout = 10000 };

            steps.Add(step);
        }

        var testData = new
        {
            userStoryId = "US-001",
            generatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"),
            description = "Playwright test script generated from selected UI elements",
            framework = "Playwright",
            configuration = new
            {
                browser = "chromium",
                headless = false,
                viewport = new { width = 1920, height = 1080 }
            },
            testCases = new[]
            {
            new
            {
                testName = request.TestName,
                url = request.Url,
                priority = "high",
                tags = new[] { "automated", "ui-test" },
                steps = steps.ToArray()
            }
        }
        };

        return System.Text.Json.JsonSerializer.Serialize(testData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    private string GetBestSelectorForElement(PageElement element)
    {
        // Use name attribute (best for form elements)
        if (!string.IsNullOrEmpty(element.Name) && !element.Name.Contains(":"))
            return $"[name='{element.Name}']";

        // Use ID only if valid
        if (!string.IsNullOrEmpty(element.Id) && IsValidCssId(element.Id))
            return $"#{element.Id}";

        // Use data-testid
        if (!string.IsNullOrEmpty(element.Name))
            return $"[data-testid='{element.Name}']";

        // Use placeholder
        if (!string.IsNullOrEmpty(element.Placeholder))
            return $"{element.Type}[placeholder='{element.Placeholder}']";

        // Use XPath
        if (!string.IsNullOrEmpty(element.XPath))
            return element.XPath;

        // Use class
        if (!string.IsNullOrEmpty(element.ClassName))
        {
            var firstClass = element.ClassName.Split(' ')[0];
            return $"{element.Type}.{firstClass}";
        }

        // Use text content
        if (!string.IsNullOrEmpty(element.Label))
            return $"{element.Type}:has-text('{element.Label}')";

        return element.Type;
    }

    private bool IsValidCssId(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (char.IsDigit(id[0])) return false;
        if (id.Contains(":") || id.Contains("[") || id.Contains("]") || id.Contains(" ")) return false;
        return true;
    }
}
