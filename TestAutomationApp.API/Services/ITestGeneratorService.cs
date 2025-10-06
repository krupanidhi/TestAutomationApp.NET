namespace TestAutomationApp.API.Services;

public interface ITestGeneratorService
{
    Task<string> GenerateTestScriptAsync(string uiDescription, string testFramework, bool includeFullClass = true);
}
