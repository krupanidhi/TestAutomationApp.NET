namespace TestAutomationApp.Shared.Models;

public class TestFlow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TestFlowPage> Pages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastExecutedAt { get; set; }
    public string? LastExecutionResult { get; set; }
}

public class TestFlowPage
{
    public int Order { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<TestFlowStep> Steps { get; set; } = new();
}

public class TestFlowStep
{
    public int Order { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object>? Options { get; set; }
}