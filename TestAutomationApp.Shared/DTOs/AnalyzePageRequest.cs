using System.ComponentModel.DataAnnotations;

namespace TestAutomationApp.Shared.DTOs;

public class AnalyzePageRequest
{
    public string? Url { get; set; }
    public string? HtmlContent { get; set; }
    public string? ScreenshotBase64 { get; set; }
    public AnalysisMethod Method { get; set; } = AnalysisMethod.Html;
}

public class AnalyzePageResponse
{
    public string UiDescription { get; set; } = string.Empty;
    public List<PageElement> Elements { get; set; } = new();
    public string PageTitle { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class PageElement
{
    public string Type { get; set; } = string.Empty; // input, button, select, etc.
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ClassName { get; set; }
    public string? Placeholder { get; set; }
    public string? Label { get; set; }
    public string? XPath { get; set; }
    public bool IsRequired { get; set; }
    public string? InputType { get; set; } // text, password, email, etc.
}

public enum AnalysisMethod
{
    Html,
    Screenshot,
    Url
}
