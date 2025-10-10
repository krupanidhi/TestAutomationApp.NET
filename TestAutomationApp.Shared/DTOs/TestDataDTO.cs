namespace TestAutomationApp.Shared.DTOs;

public class TestDataSet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = "QA"; // Dev, QA, Staging, Prod
    public Dictionary<string, TestDataEntry> Data { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TestDataEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text, password, email, number, etc.
    public bool IsSecure { get; set; } = false; // If true, value is encrypted
    public string? Description { get; set; }
}

public class TestDataRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = "QA";
    public Dictionary<string, TestDataEntry> Data { get; set; } = new();
}

public class TestDataResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TestDataSet? Data { get; set; }
}

public class TestDataListResponse
{
    public List<TestDataSet> DataSets { get; set; } = new();
    public int TotalCount { get; set; }
}

// For linking test data to scenarios
public class ScenarioTestDataMapping
{
    public string ScenarioId { get; set; } = string.Empty;
    public string TestDataSetId { get; set; } = string.Empty;
    public Dictionary<string, string> FieldMappings { get; set; } = new(); // elementKey -> testDataKey
}
