namespace TestAutomationApp.Shared.DTOs;

public class ExecuteTestRequest
{
    public string JsonScript { get; set; } = string.Empty;
    public bool Headless { get; set; } = false;
    public bool CaptureScreenshots { get; set; } = true;
    public bool CaptureVideo { get; set; } = false;
}

public class ExecuteTestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<StepResult> StepResults { get; set; } = new();
    public List<string> Screenshots { get; set; } = new();
    public string? VideoPath { get; set; }
    public double DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class StepResult
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Screenshot { get; set; }
    public double DurationMs { get; set; }
}

public class AppendPageRequest
{
    public string CurrentJson { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public List<SelectedElement> SelectedElements { get; set; } = new();
}