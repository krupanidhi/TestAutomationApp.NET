# 🎯 Complete End-to-End Example: Test Data Integration

## Overview

This guide shows you how to use the **Test Data Manager** with **Scenario Builder** to create automated tests with secure, reusable test data.

---

## 📋 Step-by-Step Walkthrough

### **Step 1: Start the Applications**

```powershell
# Terminal 1 - API Backend
cd C:\Users\KPeterson\SECURITY\RangVersion\TestAutomationApp.NET\TestAutomationApp.API
dotnet run

# Terminal 2 - Web Frontend
cd C:\Users\KPeterson\SECURITY\RangVersion\TestAutomationApp.NET\TestAutomationApp.Web
dotnet run
```

Open browser: `http://localhost:5000`

---

### **Step 2: Create Test Data Set**

1. Click **"🗄️ Test Data"** tab
2. Click **"➕ Create New Test Data Set"**
3. Fill in the form:

```
Name: Admin User - QA
Description: Admin credentials for QA environment
Environment: QA

Data Entries:
┌─────────────┬──────────────────────┬──────────┬────────┐
│ Key         │ Value                │ Type     │ Secure │
├─────────────┼──────────────────────┼──────────┼────────┤
│ username    │ asrilam              │ text     │ ☐      │
│ password    │ 3ePkpVBMzya4aICkLRyB │ password │ ☑      │
│ email       │ admin@example.com    │ email    │ ☐      │
│ firstname   │ John                 │ text     │ ☐      │
│ lastname    │ Doe                  │ text     │ ☐      │
└─────────────┴──────────────────────┴──────────┴────────┘
```

4. Click **"💾 Save"**

**Result:** Test data set is saved with encrypted password!

---

### **Step 3: Create Scenario with Test Data**

1. Click **"🎬 Multi-Page Scenario"** tab
2. Fill in scenario details:

```
Scenario Name: Login and Update Profile
Description: Login to system and update user profile information
```

3. **Select Test Data Set:**
   - Dropdown: **"Admin User - QA (QA)"**
   - ✅ Message appears: "Test data will be automatically applied to matching fields"

4. **Enter URLs:**
```
https://ehbsec.hrsa.gov/EAuthNS/internal/account/SignIn
https://ehbsec.hrsa.gov/2010/WebEPSInternal/Interface/Common/AccessControl/ViewUpdateProfile.aspx
```

5. Click **"✨ Analyze Pages & Generate Playwright JSON"**

---

### **Step 4: Review Auto-Populated Values**

After analysis completes, you'll see the **"📋 Select Elements to Include"** section.

**Notice the magic! 🎩✨**

```
Step 1: Signin
├── ☑ Username - type
│   Test Value: [asrilam]              ← Auto-populated from test data!
│   
├── ☑ Password - type
│   Test Value: [********]             ← Auto-populated (encrypted)!
│   
└── ☑ Log in - click [Navigation]

Step 2: Viewupdateprofile
├── ☑ Email - type
│   Test Value: [admin@example.com]    ← Auto-populated from test data!
│   
├── ☑ First Name - type
│   Test Value: [John]                 ← Auto-populated from test data!
│   
├── ☑ Last Name - type
│   Test Value: [Doe]                  ← Auto-populated from test data!
│   
└── ☑ Continue - click [Navigation]
```

**How it works:**
- System matches element names (Username, Password, Email, etc.)
- Looks up values in selected test data set
- Auto-populates the fields!

---

### **Step 5: Customize (Optional)**

You can still manually edit any value:

1. Uncheck elements you don't want
2. Edit values in the text boxes
3. Manual values override test data

---

### **Step 6: Generate Final JSON**

Click **"🔄 Regenerate JSON with Selected Elements"**

**Generated JSON:**

```json
{
  "scenarioName": "Login and Update Profile",
  "description": "Login to system and update user profile information",
  "testDataSetId": "abc-123-def",
  "steps": [
    {
      "order": 1,
      "pageName": "Signin",
      "pageUrl": "https://ehbsec.hrsa.gov/EAuthNS/internal/account/SignIn",
      "actions": [
        {
          "order": 1,
          "element": "Username",
          "action": "type",
          "value": "asrilam",
          "selector": "#username"
        },
        {
          "order": 2,
          "element": "Password",
          "action": "type",
          "value": "3ePkpVBMzya4aICkLRyB",
          "selector": "#password"
        },
        {
          "order": 3,
          "element": "Log in",
          "action": "click",
          "selector": ".loginButton",
          "isNavigation": true
        }
      ]
    },
    {
      "order": 2,
      "pageName": "Viewupdateprofile",
      "pageUrl": "https://ehbsec.hrsa.gov/2010/WebEPSInternal/Interface/Common/AccessControl/ViewUpdateProfile.aspx",
      "actions": [
        {
          "order": 1,
          "element": "Email",
          "action": "type",
          "value": "admin@example.com",
          "selector": "#email"
        },
        {
          "order": 2,
          "element": "First Name",
          "action": "type",
          "value": "John",
          "selector": "#firstName"
        },
        {
          "order": 3,
          "element": "Last Name",
          "action": "type",
          "value": "Doe",
          "selector": "#lastName"
        },
        {
          "order": 4,
          "element": "Continue",
          "action": "click",
          "selector": ".continueButton",
          "isNavigation": true
        }
      ]
    }
  ]
}
```

7. Click **"📋 Copy JSON"**

---

## 🎯 Key Benefits Demonstrated

### **Before (Manual Entry)**
```
❌ Type "asrilam" manually for username
❌ Type "3ePkpVBMzya4aICkLRyB" manually for password
❌ Type "admin@example.com" manually for email
❌ Repeat for every test scenario
❌ Password visible in plain text
```

### **After (Test Data Integration)**
```
✅ Select "Admin User - QA" from dropdown
✅ All fields auto-populated automatically
✅ Password encrypted and secure
✅ Change once, updates all scenarios
✅ Easy to switch environments (QA → Prod)
```

---

## 🔄 Reusing Test Data

### **Scenario 1: Login Test**
```
Test Data: Admin User - QA
URLs: https://example.com/login
Result: Username & Password auto-filled
```

### **Scenario 2: Profile Update Test**
```
Test Data: Admin User - QA  ← Same test data!
URLs: https://example.com/login, https://example.com/profile
Result: All fields auto-filled from same data set
```

### **Scenario 3: Different User**
```
Test Data: Regular User - QA  ← Different test data!
URLs: https://example.com/login
Result: Different credentials auto-filled
```

---

## 🌍 Environment Switching

### **Testing in QA**
```
Test Data Set: Admin User - QA
Environment: QA
Username: qa_admin
Password: qa_password
```

### **Testing in Production**
```
Test Data Set: Admin User - Prod
Environment: Prod
Username: prod_admin
Password: prod_password
```

**Same test scenario, different data!**

---

## 🔒 Security Features

### **Encrypted Storage**
```
Plain Text Input:  "MySecurePassword123"
                   ↓
Encrypted Storage: "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=..."
                   ↓
UI Display:        "********"
                   ↓
Test Execution:    "MySecurePassword123" (decrypted)
```

### **No Credentials in Source Control**
```
✅ Test scenarios reference test data by ID
✅ Actual credentials stored separately
✅ Encrypted at rest
✅ Can be excluded from git
```

---

## 📊 Matching Logic

The system automatically matches element names to test data keys:

### **Exact Match**
```
Element: "username" → Test Data Key: "username" ✅
Element: "password" → Test Data Key: "password" ✅
```

### **Partial Match (Case-Insensitive)**
```
Element: "Username" → Test Data Key: "username" ✅
Element: "EMAIL" → Test Data Key: "email" ✅
Element: "First Name" → Test Data Key: "firstname" ✅
```

### **Fallback**
```
Element: "SomeField" → No match → "TODO: Add value" (manual entry)
```

---

## 🎬 Complete Workflow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Create Test Data Set                                     │
│    ├── Name: Admin User - QA                                │
│    ├── Username: asrilam                                    │
│    └── Password: *** (encrypted)                            │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Create Scenario                                          │
│    ├── Select Test Data: "Admin User - QA"                 │
│    └── Enter URLs                                           │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Analyze Pages                                            │
│    ├── System finds: Username, Password fields             │
│    └── Auto-matches with test data                         │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. Review & Customize                                       │
│    ├── Username: [asrilam] ✅ Auto-filled                  │
│    ├── Password: [********] ✅ Auto-filled                 │
│    └── Edit if needed                                       │
└────────────────────┬────────────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. Generate JSON                                            │
│    ├── Contains actual values                               │
│    ├── Ready to execute                                     │
│    └── Copy & use!                                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 💡 Pro Tips

### **Tip 1: Naming Convention**
Use consistent naming for test data keys:
```
✅ Good: username, password, email, firstname, lastname
❌ Bad: user_name, pwd, e-mail, first_name, last_name
```

### **Tip 2: Multiple Environments**
Create separate test data sets for each environment:
```
├── Admin User - Dev
├── Admin User - QA
├── Admin User - Staging
└── Admin User - Prod
```

### **Tip 3: Different User Roles**
Create test data sets for different user types:
```
├── Admin User - QA
├── Regular User - QA
├── Read-Only User - QA
└── Guest User - QA
```

### **Tip 4: Secure Everything Sensitive**
Mark these as secure (encrypted):
```
✅ Passwords
✅ API Keys
✅ Credit Card Numbers
✅ SSN
✅ Any PII
```

---

## 🚀 What's Next?

Now that you have Test Data integrated with Scenario Builder, the next enhancements are:

1. **Test Execution from UI**
   - Click "▶️ Run Test" button
   - See live execution results
   - Screenshots on failure

2. **Page Object Repository**
   - Save analyzed pages
   - Reuse across scenarios
   - Version control

3. **Advanced Features**
   - Data-driven testing (run same test with multiple data sets)
   - Environment-specific execution
   - Scheduled test runs
   - CI/CD integration

---

## 🎉 Summary

You now have a **complete, integrated test automation platform** with:

✅ **Secure test data management** with encryption
✅ **Auto-population** of test values
✅ **Reusable test data** across scenarios
✅ **Environment management** (Dev, QA, Prod)
✅ **No hardcoded credentials**
✅ **Professional UI** for everything

**Time saved:** Instead of manually entering credentials in every test, you select a test data set once and everything is auto-filled!

**Security improved:** Passwords are encrypted, never exposed in plain text, and can be excluded from source control.

**Maintainability:** Change credentials once, updates all tests automatically.

---

## 📞 Need Help?

Check the logs in browser console (F12) to see:
- Test data loading
- Value matching
- Auto-population logic

Happy Testing! 🚀
