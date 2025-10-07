namespace TestAutomationApp.Shared.DTOs;

public class GenerateJsonTestRequest
{
    public string TestName { get; set; } = "Test Case";
    public string Url { get; set; } = string.Empty;
    public List<SelectedElement> SelectedElements { get; set; } = new();
}

public class SelectedElement
{
    public PageElement Element { get; set; } = new();
    public string ActionType { get; set; } = "fill"; // fill, click, select, check, verify
    public string ActionDescription { get; set; } = string.Empty;
    public string? TestValue { get; set; }
    public int Order { get; set; }
}

public class GenerateJsonTestResponse
{
    public string JsonScript { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}