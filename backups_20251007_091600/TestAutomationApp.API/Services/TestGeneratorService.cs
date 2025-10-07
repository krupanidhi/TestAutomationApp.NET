using Azure.AI.OpenAI;
using Azure;

namespace TestAutomationApp.API.Services;

public class TestGeneratorService : ITestGeneratorService
{
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient? _openAIClient;
    private readonly ILogger<TestGeneratorService> _logger;

    private class FormField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? Label { get; set; }
        public string? Placeholder { get; set; }
        public int? MaxLength { get; set; }
        public int? MinLength { get; set; }
        public string? Pattern { get; set; }
        public string[]? Options { get; set; }
    }

    public TestGeneratorService(IConfiguration configuration, ILogger<TestGeneratorService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _openAIClient = new OpenAIClient(apiKey);
            _logger.LogInformation("OpenAI integration enabled");
        }
        else
        {
            _logger.LogWarning("OpenAI API key not found. Using template-based generation.");
        }
    }

    public async Task<string> GenerateTestScriptAsync(string uiDescription, string testFramework, bool includeFullClass = true)
    {
        string result;
        if (_openAIClient != null)
        {
            result = await GenerateWithAIAsync(uiDescription, testFramework);
        }
        else
        {
            result = GenerateWithTemplate(uiDescription, testFramework);
        }

        return includeFullClass ? result : ExtractTestMethods(result);
    }

    private async Task<string> GenerateWithAIAsync(string uiDescription, string testFramework)
    {
        if (_openAIClient == null)
        {
            _logger.LogWarning("OpenAI client is not initialized. Using template-based generation.");
            return GenerateWithTemplate(uiDescription, testFramework);
        }

        try
        {
            var prompt = $@"Generate automated test scripts for the following UI:

UI Description: {uiDescription}

Test Framework: {testFramework}

Requirements:
1. Include positive and negative test scenarios
2. Add proper assertions and validations
3. Follow best practices for {testFramework}
4. Include setup and teardown methods
5. Make tests maintainable and readable
6. Use proper naming conventions
7. Add comments where necessary";

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = "gpt-4",
                Messages =
                {
                    new ChatRequestSystemMessage("You are an expert test automation engineer. Generate high-quality, production-ready test scripts."),
                    new ChatRequestUserMessage(prompt)
                },
                Temperature = 0.3f,
                MaxTokens = 3000
            };

            var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test with AI");
            return GenerateWithTemplate(uiDescription, testFramework);
        }
    }

    private string GenerateWithTemplate(string uiDescription, string testFramework, bool includeFullClass = true)
    {
        var result = testFramework switch
        {
            "Selenium WebDriver (C#)" => GenerateSeleniumCSharpTest(uiDescription),
            "Playwright (C#)" => GeneratePlaywrightCSharpTest(uiDescription),
            "XUnit + Selenium" => GenerateXUnitSeleniumTest(uiDescription),
            "NUnit + Selenium" => GenerateNUnitSeleniumTest(uiDescription),
            "SpecFlow + Selenium" => GenerateSpecFlowTest(uiDescription),
            _ => GenerateSeleniumCSharpTest(uiDescription)
        };
        
        return includeFullClass ? result : ExtractTestMethods(result);
    }
    
    private string ExtractTestMethods(string fullTestClass)
    {
        // Extract content between the opening and closing braces of the test class
        int startIndex = fullTestClass.IndexOf("{", fullTestClass.IndexOf("class")) + 1;
        int endIndex = fullTestClass.LastIndexOf('}');
        
        if (startIndex > 0 && endIndex > startIndex)
        {
            string classContent = fullTestClass.Substring(startIndex, endIndex - startIndex);
            
            // Find the first test method
            int firstTestIndex = classContent.IndexOf("[Fact]");
            if (firstTestIndex >= 0)
            {
                return classContent.Substring(firstTestIndex).Trim();
            }
        }
        
        // Fallback: return the original content if extraction fails
        return fullTestClass;
    }

    private string ExtractUrlFromDescription(string uiDescription)
    {
        // Try to find URL in the description
        var lines = uiDescription.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("URL:", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("url:", StringComparison.OrdinalIgnoreCase))
            {
                var url = line.Split(new[] { "URL:", "url:" }, StringSplitOptions.None)[1].Trim();
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    return url;
                }
            }
            
            // Also try to find URLs in the text
            if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var url = line.Trim();
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    return url;
                }
            }
        }
        
        // Default fallback
        return "https://your-application-url.com";
    }

    private string GenerateSeleniumCSharpTest(string uiDescription)
    {
        var testUrl = ExtractUrlFromDescription(uiDescription);
        
        // Extract a meaningful test class name from the UI description or URL
        string className = "FormTests";
        if (!string.IsNullOrEmpty(testUrl))
        {
            // Try to extract a meaningful name from the URL
            var uri = new Uri(testUrl);
            var segments = uri.Segments
                .Where(s => s.Length > 1)
                .Select(s => s.Trim('/'))
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.Contains("."))
                .ToList();
                
            if (segments.Count > 0)
            {
                className = string.Join("", segments
                    .Select(s => char.ToUpper(s[0]) + s.Substring(1).ToLower())
                    .Select(s => new string(s.Where(char.IsLetterOrDigit).ToArray()))
                    .Where(s => !string.IsNullOrEmpty(s)));
                className = className + "Tests";
            }
        }
        
        // Extract form name from UI description if available
        string formName = "Form";
        if (!string.IsNullOrEmpty(uiDescription))
        {
            // Look for common form names in the description
            var formNameMatch = System.Text.RegularExpressions.Regex.Match(
                uiDescription, 
                "(form|page|screen)\\s+['\"]([^'\"]+)['\"]|(login|register|contact|checkout|payment|profile|settings)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
            if (formNameMatch.Success)
            {
                formName = formNameMatch.Groups.Cast<System.Text.RegularExpressions.Group>()
                    .Skip(1)
                    .First(g => !string.IsNullOrEmpty(g.Value))
                    .Value;
                formName = char.ToUpper(formName[0]) + formName.Substring(1).ToLower();
            }
        }
        
        
        // Build the test code as a string with proper escaping
        var testCode = new System.Text.StringBuilder();
        testCode.AppendLine("using OpenQA.Selenium;");
        testCode.AppendLine("using OpenQA.Selenium.Chrome;");
        testCode.AppendLine("using OpenQA.Selenium.Support.UI;");
        testCode.AppendLine("using Xunit;");
        testCode.AppendLine("using System;");
        testCode.AppendLine();
        testCode.AppendLine("namespace AutomatedTests");
        testCode.AppendLine("{");
        testCode.AppendLine($"    public class {className} : IDisposable");
        testCode.AppendLine("    {");
        testCode.AppendLine("        private readonly IWebDriver _driver;");
        testCode.AppendLine("        private readonly WebDriverWait _wait;");
        testCode.AppendLine($"        private const string BaseUrl = \"{testUrl}\";");
        testCode.AppendLine();
        testCode.AppendLine("        public " + className + "()");
        testCode.AppendLine("        {");
        testCode.AppendLine("            _driver = new ChromeDriver();");
        testCode.AppendLine("            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));");
        testCode.AppendLine("            _driver.Manage().Window.Maximize();");
        testCode.AppendLine("        ");
        testCode.AppendLine();
        
        // Parse form fields from UI description
        var formFields = ParseFormFields(uiDescription);
        
        // Generate positive test case
        testCode.AppendLine(GeneratePositiveTestCase(testUrl, formFields));
        testCode.AppendLine();
        
        // Generate validation test cases
        testCode.AppendLine(GenerateRequiredFieldsTest(testUrl, formFields));
        testCode.AppendLine();
        
        // Add email validation test if there are email fields
        if (formFields.Any(f => f.Type == "email"))
        {
            testCode.AppendLine(GenerateEmailValidationTest(testUrl, formFields));
            testCode.AppendLine();
        }
        
        // Add radio button test if there are radio buttons
        if (formFields.Any(f => f.Type == "radio"))
        {
            testCode.AppendLine(GenerateRadioButtonTest(testUrl, formFields));
            testCode.AppendLine();
        }
        
        // Add checkbox test if there are checkboxes
        if (formFields.Any(f => f.Type == "checkbox"))
        {
            testCode.AppendLine(GenerateCheckboxTest(testUrl, formFields));
            testCode.AppendLine();
        }
        
        // Add max length test for text fields
        var textFields = formFields.Where(f => f.Type == "text" && f.MaxLength.HasValue).ToList();
        if (textFields.Any())
        {
            testCode.AppendLine(GenerateMaxLengthTest(testUrl, textFields.First()));
            testCode.AppendLine();
        }
        
        // Add form reset test
        testCode.AppendLine(GenerateFormResetTest(testUrl, formFields));
        
        // Add Dispose method
        testCode.AppendLine("        public void Dispose()");
        testCode.AppendLine("        {");
        testCode.AppendLine("            _driver?.Quit();");
        testCode.AppendLine("            _driver?.Dispose();");
        testCode.AppendLine("            GC.SuppressFinalize(this);");
        testCode.AppendLine("        }");
        
        // Close the class and namespace
        testCode.AppendLine("    }");
        testCode.AppendLine("}");
        
        return testCode.ToString();
    }
    
    private List<FormField> ParseFormFields(string uiDescription)
    {
        var fields = new List<FormField>();
        
        // Simple parsing logic - this should be enhanced based on your actual UI description format
        var lines = uiDescription.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("Field:"))
            {
                var parts = line.Split(':');
                if (parts.Length >= 3)
                {
                    var field = new FormField
                    {
                        Name = parts[1].Trim(),
                        Type = parts[2].Trim().ToLower()
                    };
                    
                    if (parts.Length > 3)
                    {
                        field.IsRequired = parts[3].Trim().Equals("required", StringComparison.OrdinalIgnoreCase);
                    }
                    
                    // Extract additional properties
                    for (int i = 4; i < parts.Length; i++)
                    {
                        var prop = parts[i].Trim();
                        if (prop.StartsWith("label="))
                            field.Label = prop[6..];
                        else if (prop.StartsWith("maxlength="))
                            field.MaxLength = int.Parse(prop[10..]);
                        else if (prop.StartsWith("options="))
                            field.Options = prop[8..].Split(',');
                        else if (prop.StartsWith("placeholder="))
                            field.Placeholder = prop[12..].Trim('\"');
                        else if (prop.StartsWith("pattern="))
                            field.Pattern = prop[8..].Trim('\"');
                        else if (prop.StartsWith("minlength="))
                            field.MinLength = int.Parse(prop[11..]);
                        else if (prop.StartsWith("maxlength="))
                            field.MaxLength = int.Parse(prop[10..]);
                    }
                    
                    fields.Add(field);
                }
            }
        }
        
        return fields;
    }
    
    private string GeneratePositiveTestCase(string testUrl, List<FormField> fields)
    {
        var testCode = new List<string>
        {
            "[Fact]",
            "public void Test_SubmitForm_WithValidData_ShouldSucceed()",
            "{",
            "    try",
            "    {",
            "        // Arrange",
            $"        _driver.Navigate().GoToUrl(\"{testUrl}\");",
            "        _wait.Until(d => d.FindElement(By.CssSelector(\"form\")));",
            "        ",
            "        // Act - Fill form with valid data"
        };
        
        // Generate test data for each field
        foreach (var field in fields)
        {
            switch (field.Type)
            {
                case "text":
                    var value = field.MaxLength.HasValue 
                        ? new string('A', Math.Min(10, field.MaxLength.Value)) 
                        : $"Test {field.Name}";
                    testCode.Add($"        // {field.Label ?? field.Name}");
                    testCode.Add($"        var {field.Name}Field = _driver.FindElement(By.Id(\"{field.Name}\"));");
                    testCode.Add($"        {field.Name}Field.Clear();");
                    testCode.Add($"        {field.Name}Field.SendKeys(\"{value}\");");
                    break;
                    
                case "textarea":
                    testCode.Add($"        // {field.Label ?? field.Name}");
                    testCode.Add($"        var {field.Name}Field = _driver.FindElement(By.Id(\"{field.Name}\"));");
                    testCode.Add($"        {field.Name}Field.Clear();");
                    testCode.Add($"        {field.Name}Field.SendKeys(\"This is a test value for {field.Name}. {string.Join(" ", Enumerable.Repeat("Test text. ", 5))}\");");
                    break;
                    
                case "select":
                    testCode.Add($"        // {field.Label ?? field.Name}");
                    testCode.Add($"        var {field.Name}Dropdown = new SelectElement(_driver.FindElement(By.Id(\"{field.Name}\")));");
                    testCode.Add($"        {field.Name}Dropdown.SelectByIndex({(field.Options?.Length > 0 ? 0 : 1)}); // Select first option or index 1 if no options specified");
                    break;
                    
                case "radio":
                    testCode.Add($"        // {field.Label ?? field.Name}");
                    testCode.Add($"        var {field.Name}Option = _driver.FindElement(By.CssSelector(\"input[name='{field.Name}']:first-of-type\"));");
                    testCode.Add($"        if (!{field.Name}Option.Selected) {field.Name}Option.Click();");
                    break;
                    
                case "checkbox":
                    testCode.Add($"        // {field.Label ?? field.Name}");
                    testCode.Add($"        var {field.Name}Checkbox = _driver.FindElement(By.Id(\"{field.Name}\"));");
                    testCode.Add($"        if (!{field.Name}Checkbox.Selected) {field.Name}Checkbox.Click();");
                    break;
                    
                case "email":
                    testCode.Add($"        // {field.Label ?? field.Name}");
                    testCode.Add($"        _driver.FindElement(By.Id(\"{field.Name}\")).SendKeys(\"test.user@example.com\");");
                    break;
                    
                default:
                    testCode.Add($"        // Unsupported field type: {field.Type} for {field.Name}");
                    break;
            }
        }
        
        testCode.AddRange(new[]
        {
            "        ",
            "        // Submit form",
            "        var submitButton = _driver.FindElement(By.CssSelector(\"button[type='submit'], input[type='submit']\"));",
            "        submitButton.Click();",
            "        ",
            "        // Assert",
            "        _wait.Until(d => d.FindElement(By.CssSelector(\".success-message, [data-testid='success-message']\")));",
            "        var successMessage = _driver.FindElement(By.CssSelector(\".success-message, [data-testid='success-message']\")).Text;",
            "        Assert.Contains(\"success\", successMessage, StringComparison.OrdinalIgnoreCase);",
            "    }",
            "    catch (Exception ex)",
            "    {",
            "        // Take screenshot on failure",
            "        var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();",
            "        var timestamp = DateTime.Now.ToString(\"yyyyMMddHHmmss\");",
            "        var screenshotPath = Path.Combine(Directory.GetCurrentDirectory(), \"TestFailure_\" + timestamp + \".png\");",
            "        screenshot.SaveAsFile(screenshotPath);",
            "        _logger.LogError(ex, \"Test failed. Screenshot saved to {0}\", screenshotPath);",
            "        throw;",
            "    }",
            "}"
        });
        
        return string.Join("\n", testCode);
    }
    
    private string GenerateRequiredFieldsTest(string testUrl, List<FormField> fields)
    {
        var requiredFields = fields.Where(f => f.IsRequired).ToList();
        if (!requiredFields.Any()) return string.Empty;
        
        var testCode = new List<string>
        {
            "[Fact]",
            "public void Test_RequiredFields_ShouldShowValidation()",
            "{",
            "    // Arrange",
            $"    _driver.Navigate().GoToUrl(\"{testUrl}\");",
            "    ",
            "    // Act - Submit without filling required fields",
            "    _driver.FindElement(By.CssSelector(\"button[type='submit']\")).Click();",
            "    ",
            "    // Assert - Check for validation messages"
        };
        
        foreach (var field in requiredFields.Take(1)) // Just test the first required field
        {
            testCode.Add($"    var {field.Name}Input = _driver.FindElement(By.Id(\"{field.Name}\"));");
            testCode.Add("    var validationMessage = " + $"{field.Name}Input.GetAttribute(\"validationMessage\");");
            testCode.Add("    Assert.NotEmpty(validationMessage);");
        }
        
        testCode.Add("}");
        return string.Join("\n", testCode);
    }
    
    private string GenerateEmailValidationTest(string testUrl, List<FormField> fields)
    {
        var emailField = fields.FirstOrDefault(f => f.Type == "email");
        if (emailField == null) return string.Empty;
        
        return $@"[Fact]
public void Test_EmailField_WithInvalidFormat_ShouldShowValidation()
{{
    // Arrange
    _driver.Navigate().GoToUrl(""{testUrl}"");
    
    // Act
    var emailField = _driver.FindElement(By.Id(""{emailField.Name}""));
    emailField.SendKeys(""invalid-email"");
    emailField.SendKeys(Keys.Tab);
    
    // Assert
    var isValid = (bool)((IJavaScriptExecutor)_driver)
        .ExecuteScript(""return arguments[0].checkValidity();"", emailField);
    Assert.False(isValid);
}}";
    }
    
    private string GenerateRadioButtonTest(string testUrl, List<FormField> fields)
    {
        var radioField = fields.First(f => f.Type == "radio");
        
        return $@"[Fact]
public void Test_RadioButtons_ShouldSelectCorrectly()
{{
    // Arrange
    _driver.Navigate().GoToUrl(""{testUrl}"");
    
    // Act
    var radioButton = _driver.FindElement(By.Id(""{radioField.Name}-option1""));
    radioButton.Click();
    
    // Assert
    Assert.True(radioButton.Selected);
}}";
    }
    
    private string GenerateMaxLengthTest(string testUrl, FormField textField)
    {
        if (!textField.MaxLength.HasValue) return string.Empty;
        
        return $@"[Fact]
public void Test_{textField.Name}_MaxLength_ShouldEnforce()
{{
    try
    {{
        // Arrange
        _driver.Navigate().GoToUrl(""{testUrl}"");
        _wait.Until(d => d.FindElement(By.Id(""{textField.Name}"")));
        
        // Act
        var textFieldElement = _driver.FindElement(By.Id(""{textField.Name}""));
        var longString = new string('A', textField.MaxLength.Value + 10);
        textFieldElement.Clear();
        textFieldElement.SendKeys(longString);
        
        // Assert
        var actualValue = textFieldElement.GetAttribute(""value"");
        Assert.True(actualValue.Length <= textField.MaxLength.Value, 
            $""{{textField.Name}} should not exceed {{textField.MaxLength}} characters. Actual: {{actualValue.Length}}"");
    }}
    catch (Exception ex)
    {{
        _logger.LogError(ex, $""Test failed for {textField.Name} max length validation"");
        throw;
    }}
}}";
    }
    
    private string GenerateCheckboxTest(string testUrl, List<FormField> fields)
    {
        var checkboxField = fields.First(f => f.Type == "checkbox");
        
        return $@"[Fact]
public void Test_Checkbox_{checkboxField.Name}_ShouldToggle()
{{
    try
    {{
        // Arrange
        _driver.Navigate().GoToUrl(""{testUrl}"");
        _wait.Until(d => d.FindElement(By.Id(""{checkboxField.Name}"")));
        
        // Act & Assert - Test checking
        var checkbox = _driver.FindElement(By.Id(""{checkboxField.Name}""));
        
        // Check the checkbox if not already checked
        if (!checkbox.Selected)
        {{
            checkbox.Click();
            Assert.True(checkbox.Selected, $""{{checkboxField.Name}} should be selected after click"");
        }}
        
        // Uncheck the checkbox
        checkbox.Click();
        Assert.False(checkbox.Selected, $""{{checkboxField.Name}} should not be selected after second click"");
        
        // Check it again
        checkbox.Click();
        Assert.True(checkbox.Selected, $""{{checkboxField.Name}} should be selected after third click"");
    }}
    catch (Exception ex)
    {{
        _logger.LogError(ex, $""Test failed for checkbox {checkboxField.Name}"");
        throw;
    }}
}}";
    }
    
    private string GenerateFormResetTest(string testUrl, List<FormField> fields)
    {{
        var formFields = fields.Where(f => f.Type != "button" && f.Type != "submit").ToList();
        if (!formFields.Any()) return string.Empty;
        
        var testCode = new List<string>
        {
            "[Fact]",
            "public void Test_FormReset_ShouldClearAllFields()",
            "{",
            "    try",
            "    {",
            "        // Arrange",
            $"        _driver.Navigate().GoToUrl(\"{testUrl}\");",
            "        _wait.Until(d => d.FindElement(By.CssSelector(\"form\")));",
            "        ",
            "        // Fill form with test data"
        };
        
        // Fill the form
        foreach (var field in formFields.Take(3)) // Limit to first 3 fields for this test
        {
            switch (field.Type)
            {
                case "text":
                case "email":
                    testCode.Add($"        _driver.FindElement(By.Id(\"{field.Name}\")).SendKeys(\"TestData\");");
                    break;
                case "select":
                    testCode.Add($"        new SelectElement(_driver.FindElement(By.Id(\"{field.Name}\"))).SelectByIndex(1);");
                    break;
                case "radio":
                    testCode.Add($"        _driver.FindElement(By.CssSelector(\"input[name='{field.Name}']:first-of-type\")).Click();");
                    break;
                case "checkbox":
                    testCode.Add($"        _driver.FindElement(By.Id(\"{field.Name}\")).Click();");
                    break;
            }
        }
        
        testCode.AddRange(new[]
        {
            "        ",
            "        // Act - Reset form",
            "        _driver.FindElement(By.CssSelector(\"button[type='reset'], input[type='reset']\")).Click();",
            "        ",
            "        // Assert - Verify fields are reset"
        });
        
        // Verify reset for each field
        foreach (var field in formFields.Take(3)) // Limit to first 3 fields for this test
        {
            switch (field.Type)
            {
                case "text":
                case "email":
                    testCode.Add($"        Assert.Equal(string.Empty, _driver.FindElement(By.Id(\"{field.Name}\")).GetAttribute(\"value\"));");
                    break;
                case "select":
                    testCode.Add($"        Assert.True(new SelectElement(_driver.FindElement(By.Id(\"{field.Name}\"))).SelectedOption.GetAttribute(\"value\") == string.Empty || \n            new SelectElement(_driver.FindElement(By.Id(\"{field.Name}\"))).SelectedOption.GetAttribute(\"value\") == \"0\");");
                    break;
                case "radio":
                    testCode.Add($"        Assert.False(_driver.FindElements(By.CssSelector(\"input[name='{field.Name}']:checked\")).Any(), \"No radio button should be selected after reset\");");
                    break;
                case "checkbox":
                    testCode.Add($"        Assert.False(_driver.FindElement(By.Id(\"{field.Name}\")).Selected, \"Checkbox should be unchecked after reset\");");
                    break;
            }
        }
        
        testCode.AddRange(new[]
        {
            "    }",
            "    catch (Exception ex)",
            "    {",
            "        _logger.LogError(ex, \"Form reset test failed\");",
            "        throw;",
            "    }",
            "}"
        });
        
        return string.Join("\n", testCode);
    }
    }
    private string GeneratePlaywrightCSharpTest(string uiDescription)
    {
        var testUrl = ExtractUrlFromDescription(uiDescription);
        
        // Extract a meaningful test class name from the UI description or URL
        string className = "FormTests";
        if (!string.IsNullOrEmpty(testUrl))
        {
            // Try to extract a meaningful name from the URL
            var uri = new Uri(testUrl);
            var segments = uri.Segments
                .Where(s => s.Length > 1)
                .Select(s => s.Trim('/'))
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.Contains("."))
                .ToList();
                
            if (segments.Count > 0)
            {
                className = string.Join("", segments
                    .Select(s => char.ToUpper(s[0]) + s.Substring(1).ToLower())
                    .Select(s => new string(s.Where(char.IsLetterOrDigit).ToArray()))
                    .Where(s => !string.IsNullOrEmpty(s)));
                className = className + "Tests";
            }
        }
        
        // Extract form name from UI description if available
        string formName = "Form";
        if (!string.IsNullOrEmpty(uiDescription))
        {
            // Look for common form names in the description
            var formNameMatch = System.Text.RegularExpressions.Regex.Match(
                uiDescription, 
                "(form|page|screen)\\s+['\"]([^'\"]+)['\"]|(login|register|contact|checkout|payment|profile|settings)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
            if (formNameMatch.Success)
            {
                formName = formNameMatch.Groups.Cast<System.Text.RegularExpressions.Group>()
                    .Skip(1)
                    .First(g => !string.IsNullOrEmpty(g.Value))
                    .Value;
                formName = char.ToUpper(formName[0]) + formName.Substring(1).ToLower();
            }
        }
        
        // Generate a more specific test method name
        string testMethodName = $"Test_{formName}Submission_WithValidData_ShouldSucceed";
        
        // Escape the test URL for use in the string literal
        string escapedTestUrl = testUrl?.Replace("\"", "\"\"") ?? string.Empty;
        
        // Build the test code as a string with proper escaping
        var testCode = new System.Text.StringBuilder();
        testCode.AppendLine("using Microsoft.Playwright;");
        testCode.AppendLine("using Microsoft.Playwright.NUnit;");
        testCode.AppendLine("using NUnit.Framework;");
        testCode.AppendLine("using System.Threading.Tasks;");
        testCode.AppendLine();
        testCode.AppendLine("namespace AutomatedTests;");
        testCode.AppendLine();
        testCode.AppendLine("[Parallelizable(ParallelScope.Self)]");
        testCode.AppendLine("[TestFixture]");
        testCode.AppendLine($"public class {className} : PageTest");
        testCode.AppendLine("{");
        testCode.AppendLine($"    private string _testUrl = \"{escapedTestUrl}\";");
        testCode.AppendLine();
        testCode.AppendLine("    [SetUp]");
        testCode.AppendLine("    public async Task Setup()");
        testCode.AppendLine("    {");
        testCode.AppendLine("        if (!string.IsNullOrEmpty(_testUrl) && !string.Equals(_testUrl, \"string\", StringComparison.OrdinalIgnoreCase))");
        testCode.AppendLine("        {");
        testCode.AppendLine("            await Page.GotoAsync(_testUrl);");
        testCode.AppendLine("        }");
        testCode.AppendLine("    }");
        testCode.AppendLine();
        testCode.AppendLine("    [Test]");
        testCode.AppendLine($"    public async Task {testMethodName}()");
        testCode.AppendLine("    {");
        testCode.AppendLine("        // TODO: Implement test with valid data");
        testCode.AppendLine("        // Example:");
        testCode.AppendLine("        // await Page.FillAsync(\"#email\", \"test@example.com\");");
        testCode.AppendLine("        // await Page.ClickAsync(\"button[type='submit']\");");
        testCode.AppendLine("        // await Expect(Page).ToHaveURLAsync($\"{_testUrl}/success\");");
        testCode.Append("    }");
        
        return testCode.ToString();
    }

    private string GenerateXUnitSeleniumTest(string uiDescription)
    {
        return "XUnit + Selenium test generation not implemented yet.";
    }

    private string GenerateNUnitSeleniumTest(string uiDescription)
    {
        return "NUnit + Selenium test generation not implemented yet.";
    }

    private string GenerateSpecFlowTest(string uiDescription)
    {
        return "SpecFlow + Selenium test generation not implemented yet.";
    }
}
