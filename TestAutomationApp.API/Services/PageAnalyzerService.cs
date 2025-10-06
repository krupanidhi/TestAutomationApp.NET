using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Azure.AI.OpenAI;
using TestAutomationApp.Shared.DTOs;

namespace TestAutomationApp.API.Services;

public class PageAnalyzerService : IPageAnalyzerService
{
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient? _openAIClient;
    private readonly ILogger<PageAnalyzerService> _logger;
    private readonly HttpClient _httpClient;

    public PageAnalyzerService(
        IConfiguration configuration, 
        ILogger<PageAnalyzerService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        var apiKey = _configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _openAIClient = new OpenAIClient(apiKey);
        }
    }

    public async Task<AnalyzePageResponse> AnalyzePageAsync(AnalyzePageRequest request)
    {
        return request.Method switch
        {
            AnalysisMethod.Html => await AnalyzeHtmlAsync(request.HtmlContent ?? string.Empty),
            AnalysisMethod.Screenshot => await AnalyzeScreenshotAsync(request.ScreenshotBase64 ?? string.Empty),
            AnalysisMethod.Url => await AnalyzeUrlAsync(request.Url ?? string.Empty),
            _ => throw new ArgumentException("Invalid analysis method")
        };
    }

    public async Task<AnalyzePageResponse> AnalyzeHtmlAsync(string html)
    {
        // Using Task.Run to offload CPU-bound work to a background thread
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return new AnalyzePageResponse { Message = "HTML content is required" };
            }

            var response = new AnalyzePageResponse();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract page title
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            response.PageTitle = titleNode?.InnerText?.Trim() ?? "Untitled Page";

            // Extract all interactive elements
            response.Elements = ExtractElements(doc);

            // Generate UI description
            response.UiDescription = GenerateUiDescription(response.PageTitle, response.Elements);
            response.Message = $"Successfully analyzed page with {response.Elements.Count} elements";

            return response;
        });
    }

    public async Task<AnalyzePageResponse> AnalyzeScreenshotAsync(string base64Image)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
        {
            return new AnalyzePageResponse { Message = "Screenshot is required" };
        }

        if (_openAIClient == null)
        {
            return new AnalyzePageResponse 
            { 
                Message = "OpenAI API key required for screenshot analysis. Please configure in appsettings.json" 
            };
        }

        try
        {
            // Use GPT-4 Vision to analyze the screenshot
            var prompt = @"Analyze this web page screenshot and extract all UI elements for test automation.

For each element, identify:
1. Element type (input, button, dropdown, checkbox, radio, link, etc.)
2. Visible label or text
3. Any identifiable attributes (id, name, placeholder)
4. Whether it appears required
5. Input type if applicable

Format the output as a structured list of elements with their properties.
Then provide a comprehensive UI description suitable for generating automated tests.";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = "gpt-4-vision-preview",
                Messages =
                {
                    new ChatRequestSystemMessage("You are an expert in web UI analysis and test automation. Extract all testable elements from screenshots."),
                    new ChatRequestUserMessage(prompt)
                },
                Temperature = 0.2f,
                MaxTokens = 2000
            };

            var result = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var analysisText = result.Value.Choices[0].Message.Content;

            // Parse the AI response and create structured data
            var response = ParseAIAnalysis(analysisText);
            response.Message = "Successfully analyzed screenshot using AI";
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing screenshot");
            return new AnalyzePageResponse { Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<AnalyzePageResponse> AnalyzeUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new AnalyzePageResponse { Message = "URL is required" };
        }

        try
        {
            // Configure HTTP client with browser-like headers
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            
            var httpResponse = await _httpClient.SendAsync(request);
            httpResponse.EnsureSuccessStatusCode();
            
            var html = await httpResponse.Content.ReadAsStringAsync();
            
            // Check if we got actual content
            if (string.IsNullOrWhiteSpace(html) || html.Length < 100)
            {
                return new AnalyzePageResponse 
                { 
                    Message = "URL returned empty or minimal content. The page may require authentication or cookies. Try 'Paste HTML' method instead." 
                };
            }
            
            var response = await AnalyzeHtmlAsync(html);
            
            // Add warning if few elements found
            if (response.Elements.Count < 3)
            {
                response.Message = $"Successfully analyzed URL: {url} - Warning: Only {response.Elements.Count} elements found. " +
                                 "The page may use JavaScript to render content. For better results, use 'Paste HTML' method after the page loads in your browser.";
            }
            else
            {
                response.Message = $"Successfully analyzed URL: {url} - Found {response.Elements.Count} elements";
            }
            
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching URL: {Url}", url);
            return new AnalyzePageResponse 
            { 
                Message = $"Error fetching URL: {ex.Message}. The page may require authentication or be behind a firewall. Use 'Paste HTML' method instead." 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching URL: {Url}", url);
            return new AnalyzePageResponse { Message = $"Error: {ex.Message}" };
        }
    }

    private List<PageElement> ExtractElements(HtmlDocument doc)
    {
        var elements = new List<PageElement>();

        // Check for Salesforce Lightning components
        var isSalesforce = doc.DocumentNode.InnerHtml.Contains("aura") || 
                          doc.DocumentNode.InnerHtml.Contains("lightning") ||
                          doc.DocumentNode.InnerHtml.Contains("slds");

        if (isSalesforce)
        {
            // Add Salesforce-specific extraction
            ExtractSalesforceElements(doc, elements);
        }

        // Extract input fields
        var inputs = doc.DocumentNode.SelectNodes("//input");
        if (inputs != null)
        {
            foreach (var input in inputs)
            {
                var element = new PageElement
                {
                    Type = "input",
                    Id = input.GetAttributeValue("id", null),
                    Name = input.GetAttributeValue("name", null),
                    ClassName = input.GetAttributeValue("class", null),
                    Placeholder = input.GetAttributeValue("placeholder", null),
                    InputType = input.GetAttributeValue("type", "text"),
                    IsRequired = input.GetAttributeValue("required", null) != null,
                    Label = FindLabelForInput(doc, input)
                };
                elements.Add(element);
            }
        }

        // Extract buttons
        var buttons = doc.DocumentNode.SelectNodes("//button");
        if (buttons != null)
        {
            foreach (var button in buttons)
            {
                elements.Add(new PageElement
                {
                    Type = "button",
                    Id = button.GetAttributeValue("id", null),
                    Name = button.GetAttributeValue("name", null),
                    ClassName = button.GetAttributeValue("class", null),
                    Label = button.InnerText?.Trim(),
                    InputType = button.GetAttributeValue("type", "button")
                });
            }
        }

        // Extract select dropdowns
        var selects = doc.DocumentNode.SelectNodes("//select");
        if (selects != null)
        {
            foreach (var select in selects)
            {
                elements.Add(new PageElement
                {
                    Type = "select",
                    Id = select.GetAttributeValue("id", null),
                    Name = select.GetAttributeValue("name", null),
                    ClassName = select.GetAttributeValue("class", null),
                    IsRequired = select.GetAttributeValue("required", null) != null,
                    Label = FindLabelForInput(doc, select)
                });
            }
        }

        // Extract textareas
        var textareas = doc.DocumentNode.SelectNodes("//textarea");
        if (textareas != null)
        {
            foreach (var textarea in textareas)
            {
                elements.Add(new PageElement
                {
                    Type = "textarea",
                    Id = textarea.GetAttributeValue("id", null),
                    Name = textarea.GetAttributeValue("name", null),
                    ClassName = textarea.GetAttributeValue("class", null),
                    Placeholder = textarea.GetAttributeValue("placeholder", null),
                    IsRequired = textarea.GetAttributeValue("required", null) != null,
                    Label = FindLabelForInput(doc, textarea)
                });
            }
        }

        // Extract links
        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links != null)
        {
            foreach (var link in links.Take(20)) // Limit to avoid too many links
            {
                var linkText = link.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(linkText))
                {
                    elements.Add(new PageElement
                    {
                        Type = "link",
                        Id = link.GetAttributeValue("id", null),
                        ClassName = link.GetAttributeValue("class", null),
                        Label = linkText
                    });
                }
            }
        }

        return elements;
    }

    private string? FindLabelForInput(HtmlDocument doc, HtmlNode input)
    {
        var id = input.GetAttributeValue("id", null);
        if (!string.IsNullOrEmpty(id))
        {
            var label = doc.DocumentNode.SelectSingleNode($"//label[@for='{id}']");
            if (label != null)
            {
                return label.InnerText?.Trim();
            }
        }

        // Try to find parent label
        var parentLabel = input.Ancestors("label").FirstOrDefault();
        if (parentLabel != null)
        {
            return parentLabel.InnerText?.Trim();
        }

        return null;
    }

    private string GenerateUiDescription(string pageTitle, List<PageElement> elements)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{pageTitle}");
        sb.AppendLine();

        // Group elements by type
        var inputFields = elements.Where(e => e.Type == "input").ToList();
        var buttons = elements.Where(e => e.Type == "button").ToList();
        var selects = elements.Where(e => e.Type == "select").ToList();
        var textareas = elements.Where(e => e.Type == "textarea").ToList();
        var links = elements.Where(e => e.Type == "link").ToList();

        if (inputFields.Any())
        {
            sb.AppendLine("Input Fields:");
            foreach (var field in inputFields)
            {
                var desc = BuildElementDescription(field);
                sb.AppendLine($"- {desc}");
            }
            sb.AppendLine();
        }

        if (selects.Any())
        {
            sb.AppendLine("Dropdown Fields:");
            foreach (var select in selects)
            {
                var desc = BuildElementDescription(select);
                sb.AppendLine($"- {desc}");
            }
            sb.AppendLine();
        }

        if (textareas.Any())
        {
            sb.AppendLine("Text Areas:");
            foreach (var textarea in textareas)
            {
                var desc = BuildElementDescription(textarea);
                sb.AppendLine($"- {desc}");
            }
            sb.AppendLine();
        }

        if (buttons.Any())
        {
            sb.AppendLine("Buttons:");
            foreach (var button in buttons)
            {
                var desc = BuildElementDescription(button);
                sb.AppendLine($"- {desc}");
            }
            sb.AppendLine();
        }

        if (links.Any())
        {
            sb.AppendLine("Links:");
            foreach (var link in links.Take(10)) // Limit links
            {
                var desc = BuildElementDescription(link);
                sb.AppendLine($"- {desc}");
            }
            sb.AppendLine();
        }

        // Add validation section
        var requiredFields = elements.Where(e => e.IsRequired).ToList();
        if (requiredFields.Any())
        {
            sb.AppendLine("Required Fields:");
            foreach (var field in requiredFields)
            {
                sb.AppendLine($"- {field.Label ?? field.Id ?? field.Name ?? "Unknown field"}");
            }
        }

        return sb.ToString();
    }

    private string BuildElementDescription(PageElement element)
    {
        var parts = new List<string>();

        // Add label or description
        if (!string.IsNullOrEmpty(element.Label))
        {
            parts.Add(element.Label);
        }
        else if (!string.IsNullOrEmpty(element.Placeholder))
        {
            parts.Add(element.Placeholder);
        }
        else
        {
            parts.Add($"{element.Type} field");
        }

        // Add type info
        if (!string.IsNullOrEmpty(element.InputType) && element.InputType != "text")
        {
            parts.Add($"type: {element.InputType}");
        }

        // Add locators
        var locators = new List<string>();
        if (!string.IsNullOrEmpty(element.Id))
            locators.Add($"id: {element.Id}");
        if (!string.IsNullOrEmpty(element.Name))
            locators.Add($"name: {element.Name}");
        if (!string.IsNullOrEmpty(element.ClassName))
            locators.Add($"class: {element.ClassName}");

        if (locators.Any())
        {
            parts.Add($"({string.Join(", ", locators)})");
        }

        // Add required flag
        if (element.IsRequired)
        {
            parts.Add("required");
        }

        return string.Join(" ", parts);
    }

    private AnalyzePageResponse ParseAIAnalysis(string analysisText)
    {
        // Simple parsing of AI response
        // In production, you'd want more sophisticated parsing
        var response = new AnalyzePageResponse
        {
            UiDescription = analysisText,
            PageTitle = "AI Analyzed Page"
        };

        // Try to extract structured elements from AI response
        // This is a simplified version - AI responses would need better parsing
        var lines = analysisText.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("input", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("button", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("dropdown", StringComparison.OrdinalIgnoreCase))
            {
                // Extract basic info from AI description
                var element = new PageElement
                {
                    Type = DetermineElementType(line),
                    Label = line.Trim()
                };
                response.Elements.Add(element);
            }
        }

        return response;
    }

    private string DetermineElementType(string line)
    {
        var lowerLine = line.ToLower();
        if (lowerLine.Contains("button")) return "button";
        if (lowerLine.Contains("dropdown") || lowerLine.Contains("select")) return "select";
        if (lowerLine.Contains("checkbox")) return "checkbox";
        if (lowerLine.Contains("radio")) return "radio";
        if (lowerLine.Contains("textarea")) return "textarea";
        return "input";
    }

    private void ExtractSalesforceElements(HtmlDocument doc, List<PageElement> elements)
    {
        // Extract Lightning input components
        var lightningInputs = doc.DocumentNode.SelectNodes("//lightning-input");
        if (lightningInputs != null)
        {
            foreach (var input in lightningInputs)
            {
                elements.Add(new PageElement
                {
                    Type = "input",
                    Label = input.GetAttributeValue("label", null),
                    Name = input.GetAttributeValue("name", null),
                    Placeholder = input.GetAttributeValue("placeholder", null),
                    InputType = input.GetAttributeValue("type", "text"),
                    IsRequired = input.GetAttributeValue("required", null) != null
                });
            }
        }

        // Extract Lightning buttons
        var lightningButtons = doc.DocumentNode.SelectNodes("//lightning-button");
        if (lightningButtons != null)
        {
            foreach (var button in lightningButtons)
            {
                elements.Add(new PageElement
                {
                    Type = "button",
                    Label = button.GetAttributeValue("label", button.InnerText?.Trim()),
                    Name = button.GetAttributeValue("name", null),
                    ClassName = button.GetAttributeValue("class", null)
                });
            }
        }

        // Extract elements with slds classes (Salesforce Lightning Design System)
        var sldsInputs = doc.DocumentNode.SelectNodes("//input[contains(@class, 'slds-input')]");
        if (sldsInputs != null)
        {
            foreach (var input in sldsInputs)
            {
                if (!elements.Any(e => e.Name == input.GetAttributeValue("name", null)))
                {
                    elements.Add(new PageElement
                    {
                        Type = "input",
                        Name = input.GetAttributeValue("name", null),
                        Placeholder = input.GetAttributeValue("placeholder", null),
                        InputType = input.GetAttributeValue("type", "text"),
                        ClassName = "slds-input",
                        IsRequired = input.GetAttributeValue("required", null) != null
                    });
                }
            }
        }

        // Extract SLDS buttons
        var sldsButtons = doc.DocumentNode.SelectNodes("//button[contains(@class, 'slds-button')]");
        if (sldsButtons != null)
        {
            foreach (var button in sldsButtons)
            {
                elements.Add(new PageElement
                {
                    Type = "button",
                    Label = button.InnerText?.Trim(),
                    ClassName = button.GetAttributeValue("class", null)
                });
            }
        }

        // If still no elements found, add a helpful message
        if (elements.Count == 0)
        {
            _logger.LogWarning("Salesforce page detected but no elements extracted. Page may use Shadow DOM.");
        }
    }
}
