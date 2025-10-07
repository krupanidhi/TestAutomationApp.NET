# Test Automation Platform (.NET)

A comprehensive web application built with **ASP.NET Core** and **Blazor WebAssembly** that provides:

1. **Account Management Form** - Create and manage employee account requests
2. **Local SQLite Database** - Store account details locally
3. **🔥 NEW: Automated UI Analysis** - Automatically extract UI elements from any web page
4. **AI-Powered Test Script Generator** - Automatically generate test automation scripts for ANY UI

## 🚀 Features

### Account Management
- Complete employee information form based on your provided design
- Validation for all required fields
- SQLite database for local storage
- View all created accounts in a table

### 🔥 Automated UI Analysis (NEW!)
- **No Manual Work**: Automatically extract all UI elements from web pages
- **Three Methods**:
  - **Paste HTML**: Copy page source and analyze instantly
  - **Enter URL**: Fetch and analyze any accessible URL
  - **Upload Screenshot**: AI-powered visual analysis
- **Complete Extraction**: IDs, names, classes, types, required fields
- **Instant Results**: Seconds instead of minutes
- **One-Click Integration**: Auto-populate test generator

### Test Script Generator
- **Universal**: Works for ANY web application UI
- **Multi-Framework Support**:
  - Selenium WebDriver (C#)
  - Playwright (C#)
  - XUnit + Selenium
  - NUnit + Selenium
  - SpecFlow + Selenium (BDD)
- **AI-Powered**: Uses OpenAI GPT-4 (optional) or template-based generation
- **Smart Generation**: Creates comprehensive test cases with:
  - Positive and negative scenarios
  - Proper assertions and validations
  - Setup and teardown methods
  - Best practices and naming conventions

## 📋 Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022** or **Visual Studio Code**
- **Optional**: OpenAI API key for AI-powered test generation

## 🛠️ Installation & Setup

### 1. Clone or Navigate to the Project

```powershell
cd C:\Users\KPeterson\CascadeProjects\TestAutomationApp.NET
```

### 2. Restore NuGet Packages

```powershell
dotnet restore
```

### 3. Configure OpenAI (Optional)

If you want AI-powered test generation, add your OpenAI API key:

Edit `TestAutomationApp.API\appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

**Note**: Without an API key, the app will use template-based generation (still very effective!).

### 4. Build the Solution

```powershell
dotnet build
```

## ▶️ Running the Application

### Option 1: Run Both Projects Separately

**Terminal 1 - Start the API:**
```powershell
cd TestAutomationApp.API
dotnet run
```
The API will start at `http://localhost:5001`

**Terminal 2 - Start the Blazor Web App:**
```powershell
cd TestAutomationApp.Web
dotnet run
```
The web app will start at `http://localhost:5000`

### Option 2: Using Visual Studio

1. Right-click the solution in Solution Explorer
2. Select **Set Startup Projects**
3. Choose **Multiple startup projects**
4. Set both `TestAutomationApp.API` and `TestAutomationApp.Web` to **Start**
5. Click **OK** and press **F5**

### Option 3: Using VS Code

Open two terminals and run:
```powershell
# Terminal 1
cd TestAutomationApp.API
dotnet watch run

# Terminal 2
cd TestAutomationApp.Web
dotnet watch run
```

## 📖 Usage

### Creating an Account

1. Navigate to `http://localhost:5000`
2. Fill in the **Request New Account** form
3. Click **Save and Continue**
4. View created accounts in the **View Accounts** tab

### Generating Test Scripts

1. Click the **Test Script Generator** tab
2. Describe your UI in the text area (or click the example button)
3. Select your preferred test framework
4. Click **Generate Test Scripts**
5. Copy or download the generated test code
6. Run the tests in your project!

## 🗂️ Project Structure

```
TestAutomationApp.NET/
├── TestAutomationApp.API/          # ASP.NET Core Web API
│   ├── Controllers/                # API endpoints
│   ├── Data/                       # Database context
│   ├── Services/                   # Test generator service
│   └── testautomation.db          # SQLite database (created on first run)
├── TestAutomationApp.Web/          # Blazor WebAssembly
│   ├── Pages/                      # Razor components
│   └── wwwroot/                    # Static files & CSS
└── TestAutomationApp.Shared/       # Shared models & DTOs
    ├── Models/                     # Account & TestScript models
    └── DTOs/                       # Data transfer objects
```

## 🧪 Example: Using Generated Tests

The generator creates ready-to-run test code. Here's how to use it:

### 1. Create a New Test Project

```powershell
dotnet new xunit -n MyApp.Tests
cd MyApp.Tests
dotnet add package Selenium.WebDriver
dotnet add package Selenium.WebDriver.ChromeDriver
```

### 2. Copy Generated Code

Paste the generated test code into your test project.

### 3. Run Tests

```powershell
dotnet test
```

## 🔧 Configuration

### Database Location

The SQLite database is created at `TestAutomationApp.API\testautomation.db`

To change the location, edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=C:\\path\\to\\your\\database.db"
  }
}
```

### API Base URL

If you change the API port, update `TestAutomationApp.Web\Program.cs`:

```csharp
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:YOUR_PORT") 
});
```

## 🎯 Test Frameworks Supported

| Framework | Language | Description |
|-----------|----------|-------------|
| **Selenium WebDriver (C#)** | C# | Industry-standard browser automation |
| **Playwright (C#)** | C# | Modern, fast, reliable automation |
| **XUnit + Selenium** | C# | XUnit testing framework with Selenium |
| **NUnit + Selenium** | C# | NUnit testing framework with Selenium |
| **SpecFlow + Selenium** | C# (BDD) | Behavior-Driven Development with Gherkin |

## 🌟 Key Features of Generated Tests

✅ **Comprehensive Coverage**
- Form field validation
- Required field checks
- Email format validation
- Radio button/checkbox interactions
- Dropdown selections
- Max length enforcement

✅ **Best Practices**
- Arrange-Act-Assert pattern
- Proper setup and teardown
- Explicit waits
- Page Object Model ready
- Clear naming conventions

✅ **Production Ready**
- Exception handling
- Logging support
- Parallel execution support
- Cross-browser compatible

## 🔐 Security Notes

- The SQLite database is stored locally and not encrypted
- Do not commit `appsettings.json` with API keys to version control
- Use environment variables or Azure Key Vault for production

## 🐛 Troubleshooting

### Database Issues

If you encounter database errors, delete `testautomation.db` and restart the API.

### CORS Errors

Ensure the API is running before starting the web app. Check that the CORS policy in `Program.cs` matches your web app URL.

### Port Conflicts

If ports 5000 or 5001 are in use, modify `launchSettings.json` in both projects.

## 📝 API Endpoints

### Accounts
- `GET /api/accounts` - Get all accounts
- `GET /api/accounts/{id}` - Get account by ID
- `POST /api/accounts` - Create new account
- `PUT /api/accounts/{id}` - Update account
- `DELETE /api/accounts/{id}` - Delete account

### Test Scripts
- `GET /api/testscripts` - Get all generated scripts
- `GET /api/testscripts/{id}` - Get script by ID
- `POST /api/testscripts/generate` - Generate new test script
- `DELETE /api/testscripts/{id}` - Delete script

## 🚀 Next Steps

1. **Extend the Generator**: Add more test frameworks (Cypress, Puppeteer, etc.)
2. **Add Authentication**: Secure the API with JWT tokens
3. **Cloud Deployment**: Deploy to Azure App Service
4. **Export Features**: Export accounts to CSV/Excel
5. **Test Execution**: Run generated tests directly from the UI

## 📄 License

This project is provided as-is for educational and development purposes.

## 🤝 Contributing

Feel free to extend and customize this application for your needs!

---

**Built with ❤️ using ASP.NET Core 8.0 and Blazor WebAssembly**
