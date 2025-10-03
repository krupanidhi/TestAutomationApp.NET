using System.ComponentModel.DataAnnotations;

namespace TestAutomationApp.Shared.DTOs;

public class GenerateTestRequest
{
    [Required]
    public string UiDescription { get; set; } = string.Empty;

    [Required]
    public string TestFramework { get; set; } = string.Empty;
}

public class GenerateTestResponse
{
    public int Id { get; set; }
    public string Script { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
