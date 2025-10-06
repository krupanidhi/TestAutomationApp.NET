using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Services;

public interface IPageAnalyzerService
{
    Task<AnalyzePageResponse> AnalyzePageAsync(AnalyzePageRequest request);
    Task<AnalyzePageResponse> AnalyzeHtmlAsync(string html);
    Task<AnalyzePageResponse> AnalyzeScreenshotAsync(string base64Image);
    Task<AnalyzePageResponse> AnalyzeUrlAsync(string url, string? username = null, string? password = null);
}
