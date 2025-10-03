# 🚀 Quick Start Guide

Get the Test Automation Platform running in 3 minutes!

## Step 1: Open Two Terminals

### Terminal 1 - Start the API Backend
```powershell
cd C:\Users\KPeterson\CascadeProjects\TestAutomationApp.NET\TestAutomationApp.API
dotnet run
```

Wait for: `Now listening on: http://localhost:5001`

### Terminal 2 - Start the Blazor Frontend
```powershell
cd C:\Users\KPeterson\CascadeProjects\TestAutomationApp.NET\TestAutomationApp.Web
dotnet run
```

Wait for: `Now listening on: http://localhost:5000`

## Step 2: Open Your Browser

Navigate to: **http://localhost:5000**

## Step 3: Try It Out!

### Create an Account
1. Fill in the form with sample data
2. Click "Save and Continue"
3. Check the "View Accounts" tab

### Generate Test Scripts
1. Click "Test Script Generator" tab
2. Click "→ Use Account Form from this app"
3. Select a test framework (e.g., "Selenium WebDriver (C#)")
4. Click "Generate Test Scripts"
5. Copy or download the generated code!

## 🎯 What You Get

- ✅ Fully functional account management system
- ✅ Local SQLite database (auto-created)
- ✅ AI-powered test script generator
- ✅ Support for 5+ test frameworks
- ✅ Ready-to-run test code

## 🔧 Optional: Enable AI Generation

1. Get an OpenAI API key from https://platform.openai.com/api-keys
2. Edit `TestAutomationApp.API\appsettings.json`
3. Add your key:
   ```json
   {
     "OpenAI": {
       "ApiKey": "sk-your-key-here"
     }
   }
   ```
4. Restart the API

**Note**: Without an API key, template-based generation still works great!

## 📚 Next Steps

- Read the full [README.md](README.md) for detailed documentation
- Customize the form fields in `AccountForm.razor`
- Add more test framework templates in `TestGeneratorService.cs`
- Deploy to Azure or your preferred cloud platform

## ❓ Troubleshooting

**Port already in use?**
- Change ports in `Properties/launchSettings.json` in both projects

**Database errors?**
- Delete `testautomation.db` and restart the API

**CORS errors?**
- Ensure API is running before starting the web app
- Check the BaseAddress in `TestAutomationApp.Web\Program.cs`

---

**Happy Testing! 🎉**
