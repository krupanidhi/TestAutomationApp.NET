namespace TestAutomationApp.Shared.DTOs;

public class TestScenarioRequest
{
    public string ScenarioName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PageStep> Steps { get; set; } = new();
}

public class PageStep
{
    public int Order { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public List<ElementAction> Actions { get; set; } = new();
    public string? ExpectedNavigation { get; set; } // URL to navigate to after actions
    public List<Assertion>? Assertions { get; set; } // Optional validations
}

public class ElementAction
{
    public int Order { get; set; }
    public string ElementLabel { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty; // input, button, select, etc.
    public string ActionType { get; set; } = string.Empty; // click, type, select, etc.
    public string? Value { get; set; } // For type/select actions
    public string? Selector { get; set; } // CSS or XPath selector
    public int DelayAfterMs { get; set; } = 0; // Wait time after action
}

public class Assertion
{
    public string Type { get; set; } = string.Empty; // url, element, text
    public string Expected { get; set; } = string.Empty;
    public string? Selector { get; set; }
}

public class TestScenarioResponse
{
    public string ScenarioName { get; set; } = string.Empty;
    public string GeneratedScript { get; set; } = string.Empty;
    public List<PageStepResult> AnalyzedSteps { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class PageStepResult
{
    public int Order { get; set; }
    public string PageName { get; set; } = string.Empty;
    public List<PageElement> AvailableElements { get; set; } = new();
    public List<ElementAction> ConfiguredActions { get; set; } = new();
}
