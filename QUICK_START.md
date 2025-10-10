# 🚀 Quick Start Guide

## Get Started in 5 Minutes!

### **Step 1: Start the Applications** (1 minute)

Open two PowerShell terminals:

**Terminal 1 - API:**
```powershell
cd C:\Users\KPeterson\SECURITY\RangVersion\TestAutomationApp.NET\TestAutomationApp.API
dotnet run
```

**Terminal 2 - Web:**
```powershell
cd C:\Users\KPeterson\SECURITY\RangVersion\TestAutomationApp.NET\TestAutomationApp.Web
dotnet run
```

Open browser: **http://localhost:5000**

---

### **Step 2: Create Test Data** (1 minute)

1. Click **"🗄️ Test Data"** tab
2. Click **"➕ Create New Test Data Set"**
3. Fill in:
   - Name: `My First Test Data`
   - Environment: `QA`
   - Add entries:
     - Key: `username`, Value: `testuser`, Type: `text`, Secure: ☐
     - Key: `password`, Value: `testpass123`, Type: `password`, Secure: ☑
4. Click **"💾 Save"**

✅ **Done!** Your test data is now encrypted and saved.

---

### **Step 3: Create & Run Test** (3 minutes)

1. Click **"🎬 Multi-Page Scenario"** tab
2. Fill in:
   - Scenario Name: `My First Test`
   - Description: `Testing login functionality`
   - **Test Data Set**: Select `My First Test Data (QA)` ⭐
   - URLs (one per line):
     ```
     https://ehbsec.hrsa.gov/EAuthNS/internal/account/SignIn
     ```
3. Click **"✨ Analyze Pages & Generate Playwright JSON"**
4. Wait ~10 seconds for analysis
5. **Notice:** Username and Password fields are auto-filled! ⭐
6. Click **"▶️ Run Test"** ⭐
7. Wait ~15 seconds
8. **View Results** with screenshots! ⭐

✅ **Congratulations!** You just:
- Created secure test data
- Auto-generated a test
- Executed it with one click
- Got visual results!

---

## 🎯 What You Can Do Now

### **Test Data Manager** (🗄️ Tab)
- Create unlimited test data sets
- Separate data for Dev/QA/Staging/Prod
- Encrypt sensitive values (passwords, API keys)
- Edit/Delete existing data

### **Scenario Builder** (🎬 Tab)
- Analyze any web page
- Auto-detect form fields and buttons
- Select test data for auto-population ⭐
- Customize which elements to include
- Edit test values
- Generate Playwright JSON
- **Execute tests with one click** ⭐

### **Test Execution** (▶️ Button)
- Run tests directly from UI
- See live execution status
- View step-by-step results
- Check screenshots for each step
- Debug failures with error messages

---

## 💡 Pro Tips

### **Tip 1: Reuse Test Data**
Create one test data set, use it in multiple scenarios:
```
Test Data: "Admin User - QA"
├── Scenario 1: Login Test
├── Scenario 2: Profile Update Test
└── Scenario 3: Settings Test
```

### **Tip 2: Smart Matching**
The system automatically matches element names to test data:
```
Element "Username" → Test Data "username" ✅
Element "Password" → Test Data "password" ✅
Element "Email" → Test Data "email" ✅
```

### **Tip 3: Quick Execution**
1. Generate JSON once
2. Edit values as needed
3. Click "Regenerate JSON" (instant)
4. Click "Run Test" (see results)
5. Repeat steps 2-4 for rapid testing

---

## 📚 Learn More

- **Complete Guide**: `END_TO_END_EXAMPLE.md`
- **Technical Details**: `ENHANCEMENTS_SUMMARY.md`
- **Full Summary**: `COMPLETE_IMPLEMENTATION_SUMMARY.md`

---

## 🐛 Troubleshooting

**Problem**: Test data dropdown is empty
- **Solution**: Make sure API is running and you created at least one test data set

**Problem**: Values not auto-populating
- **Solution**: Select a test data set from the dropdown first

**Problem**: Test execution fails
- **Solution**: Check API terminal for errors, ensure Playwright is installed

**Problem**: Can't see screenshots
- **Solution**: Screenshots appear in execution results after test completes

---

## 🎉 You're Ready!

You now have everything you need to:
- ✅ Manage test data securely
- ✅ Generate tests automatically
- ✅ Execute tests with one click
- ✅ View results visually

**Happy Testing!** 🚀

---

## 📞 Quick Reference

### **URLs**
- Web UI: http://localhost:5000
- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger

### **Tabs**
- 🗄️ Test Data - Manage test data
- 🎬 Multi-Page Scenario - Build & run tests

### **Buttons**
- ✨ Analyze Pages - Analyze URLs and generate JSON
- 🔄 Regenerate JSON - Update JSON with selections
- ▶️ Run Test - Execute the test
- 📋 Copy JSON - Copy generated JSON

### **Features**
- 🔒 Encryption - AES-256 for passwords
- 🌍 Environments - Dev, QA, Staging, Prod
- ⚡ Auto-fill - Smart value population
- 📸 Screenshots - Visual test results
