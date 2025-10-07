# add-json-support.ps1 - Adds JSON Test Data support to Test Automation App
$ErrorActionPreference = "Stop"
$projectRoot = "c:\Users\rkodakalla\CascadeProjects\TestAutomationApp.NET"

Write-Host "`n🚀 Adding JSON Test Data support to Test Automation App..." -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Gray

# Backup files first
Write-Host "`n📦 Creating backups..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $projectRoot "backups_$timestamp"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

$filesToBackup = @(
    "TestAutomationApp.Web\Pages\TestGenerator.razor",
    "TestAutomationApp.API\Services\TestGeneratorService.cs",
    "README.md"
)

foreach ($file in $filesToBackup) {
    $sourcePath = Join-Path $projectRoot $file
    $destPath = Join-Path $backupDir $file
    $destDir = Split-Path $destPath -Parent
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    Copy-Item $sourcePath $destPath
    Write-Host "   ✓ Backed up: $file" -ForegroundColor Green
}

# File 1: Update TestGenerator.razor
Write-Host "`n1️⃣  Updating TestGenerator.razor..." -ForegroundColor Yellow
$razorFile = Join-Path $projectRoot "TestAutomationApp.Web\Pages\TestGenerator.razor"
$razorContent = Get-Content $razorFile -Raw

# Add JSON to frameworks array
$razorContent = $razorContent -replace `
    '("SpecFlow \+ Selenium"\r?\n\s*)\};', `
    '$1,"JSON Test Data"`n    };'

# Add JSON to extension switch
$razorContent = $razorContent -replace `
    '("SpecFlow \+ Selenium" => "feature",\r?\n\s*)(_ => "cs")', `
    '"SpecFlow + Selenium" => "feature",`n            "JSON Test Data" => "json",`n            $2'

Set-Content $razorFile -Value $razorContent -NoNewline
Write-Host "   ✓ Added 'JSON Test Data' to frameworks array" -ForegroundColor Green
Write-Host "   ✓ Added '.json' file extension mapping" -ForegroundColor Green

# File 2: Update TestGeneratorService.cs
Write-Host "`n2️⃣  Updating TestGeneratorService.cs..." -ForegroundColor Yellow
$serviceFile = Join-Path $projectRoot "TestAutomationApp.API\Services\TestGeneratorService.cs"
$serviceContent = Get-Content $serviceFile -Raw

# Add JSON to switch statement
$serviceContent = $serviceContent -replace `
    '("SpecFlow \+ Selenium" => GenerateSpecFlowTest\(uiDescription\),\r?\n\s*)(_ => GenerateSeleniumCSharpTest)', `
    '"SpecFlow + Selenium" => GenerateSpecFlowTest(uiDescription),`n            "JSON Test Data" => GenerateJsonTestData(uiDescription),`n            $2'

# Add new methods before closing brace
$newMethods = @'

    private string GenerateJsonTestData(string uiDescription)
    {
        var formFields = ParseFormFields(uiDescription);
        var testUrl = ExtractUrlFromDescription(uiDescription);
        
        var testData = new
        {
            metadata = new
            {
                testUrl = testUrl,
                generatedAt = DateTime.UtcNow.ToString("o"),
                description = "Auto-generated test data for UI testing",
                framework = "JSON Test Data"
            },
            testCases = new[]
            {
                new
                {
                    testCaseId = "TC001",
                    name = "Valid Form Submission - All Required Fields",
                    type = "positive",
                    priority = "high",
                    data = GenerateTestDataObject(formFields, true)
                },
                new
                {
                    testCaseId = "TC002",
                    name = "Invalid Form Submission - Missing Required Fields",
                    type = "negative",
                    priority = "high",
                    data = GenerateTestDataObject(formFields, false)
                },
                new
                {
                    testCaseId = "TC003",
                    name = "Boundary Value Testing - Max Length Fields",
                    type = "boundary",
                    priority = "medium",
                    data = GenerateBoundaryTestData(formFields)
                }
            },
            fieldDefinitions = formFields.Select(f => new
            {
                name = f.Name,
                type = f.Type,
                isRequired = f.IsRequired,
                label = f.Label ?? f.Name,
                maxLength = f.MaxLength,
                minLength = f.MinLength,
                pattern = f.Pattern,
                options = f.Options,
                placeholder = f.Placeholder
            }).ToArray()
        };
        
        return System.Text.Json.JsonSerializer.Serialize(testData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    private object GenerateTestDataObject(List<FormField> fields, bool validData)
    {
        var data = new Dictionary<string, object>();
        
        foreach (var field in fields)
        {
            if (!validData && field.IsRequired)
            {
                data[field.Name] = string.Empty;
                continue;
            }
            
            switch (field.Type.ToLower())
            {
                case "text":
                    data[field.Name] = validData 
                        ? (field.MaxLength.HasValue 
                            ? new string('A', Math.Min(10, field.MaxLength.Value))
                            : $"Test {field.Name}")
                        : string.Empty;
                    break;
                    
                case "email":
                    data[field.Name] = validData ? "test.user@example.com" : "invalid-email";
                    break;
                    
                case "select":
                case "dropdown":
                    data[field.Name] = validData 
                        ? (field.Options?.FirstOrDefault() ?? "option1")
                        : string.Empty;
                    break;
                    
                case "radio":
                    data[field.Name] = validData ? (field.Options?.FirstOrDefault() ?? "option1") : string.Empty;
                    break;
                    
                case "checkbox":
                    data[field.Name] = validData;
                    break;
                    
                case "textarea":
                    data[field.Name] = validData 
                        ? $"This is test data for {field.Name}. " + string.Join(" ", Enumerable.Repeat("Test text.", 5))
                        : string.Empty;
                    break;
                    
                case "number":
                    data[field.Name] = validData ? 123 : -1;
                    break;
                    
                case "date":
                    data[field.Name] = validData ? DateTime.Now.ToString("yyyy-MM-dd") : "invalid-date";
                    break;
                    
                case "tel":
                case "phone":
                    data[field.Name] = validData ? "555-123-4567" : "abc";
                    break;
                    
                default:
                    data[field.Name] = validData ? $"test-{field.Name}" : string.Empty;
                    break;
            }
        }
        
        return data;
    }

    private object GenerateBoundaryTestData(List<FormField> fields)
    {
        var data = new Dictionary<string, object>();
        
        foreach (var field in fields)
        {
            switch (field.Type.ToLower())
            {
                case "text":
                case "textarea":
                    if (field.MaxLength.HasValue)
                    {
                        data[field.Name] = new string('X', field.MaxLength.Value);
                    }
                    else
                    {
                        data[field.Name] = "Boundary test value";
                    }
                    break;
                    
                case "email":
                    data[field.Name] = "verylongemailaddress.test.user@example-domain-name.com";
                    break;
                    
                case "number":
                    data[field.Name] = int.MaxValue;
                    break;
                    
                default:
                    data[field.Name] = $"boundary-{field.Name}";
                    break;
            }
        }
        
        return data;
    }
'@

$serviceContent = $serviceContent -replace '(\}\r?\n)$', "$newMethods`n}"
Set-Content $serviceFile -Value $serviceContent -NoNewline
Write-Host "   ✓ Added JSON case to switch statement" -ForegroundColor Green
Write-Host "   ✓ Added GenerateJsonTestData() method" -ForegroundColor Green
Write-Host "   ✓ Added GenerateTestDataObject() method" -ForegroundColor Green
Write-Host "   ✓ Added GenerateBoundaryTestData() method" -ForegroundColor Green

# File 3: Update README.md
Write-Host "`n3️⃣  Updating README.md..." -ForegroundColor Yellow
$readmeFile = Join-Path $projectRoot "README.md"
$readmeContent = Get-Content $readmeFile -Raw

$readmeContent = $readmeContent -replace `
    '(\| \*\*SpecFlow \+ Selenium\*\* \| C# \(BDD\) \| Behavior-Driven Development with Gherkin \|)', `
    '$1`n| **JSON Test Data** | JSON | Structured test data with positive/negative/boundary cases |'

Set-Content $readmeFile -Value $readmeContent -NoNewline
Write-Host "   ✓ Added JSON Test Data to framework table" -ForegroundColor Green

Write-Host "`n" + "=" * 70 -ForegroundColor Gray
Write-Host "✅ All changes applied successfully!" -ForegroundColor Green
Write-Host "`n📁 Backup location: $backupDir" -ForegroundColor Cyan
Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Rebuild the solution:" -ForegroundColor White
Write-Host "      dotnet build" -ForegroundColor Gray
Write-Host "`n   2. Run the application:" -ForegroundColor White
Write-Host "      cd TestAutomationApp.API && dotnet run" -ForegroundColor Gray
Write-Host "      cd TestAutomationApp.Web && dotnet run" -ForegroundColor Gray
Write-Host "`n   3. Open browser and select 'JSON Test Data' from framework dropdown" -ForegroundColor White
Write-Host "`n✨ You can now generate JSON test data files!" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Gray