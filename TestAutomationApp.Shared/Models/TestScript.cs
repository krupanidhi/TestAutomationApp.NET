using System.ComponentModel.DataAnnotations;

namespace TestAutomationApp.Shared.Models;

public class TestScript
{
    public int Id { get; set; }

    [Required]
    public string UiDescription { get; set; } = string.Empty;

    [Required]
    public string TestFramework { get; set; } = string.Empty;

    [Required]
    public string GeneratedScript { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
