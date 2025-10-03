# 🌩️ Salesforce Lightning Pages - Testing Guide

## ⚠️ Special Considerations for Salesforce

Salesforce Lightning pages use **Shadow DOM** and **custom web components**, which makes automated element extraction challenging. Here's how to handle them.

---

## 🎯 Recommended Approach for Your Login Page

### **Option 1: Manual UI Description (Fastest)**

Since automated extraction doesn't work well with Salesforce Shadow DOM, **manually create the description**:

```
Salesforce PARS Login Page
URL: https://hrsa-dcpaas--dcpuat.sandbox.my.site.com/pars/s/login/

Input Fields:
- Username input (name: username, placeholder: Username, CSS: input[name='username'])
- Password input (name: password, type: password, placeholder: Password, CSS: input[type='password'])

Buttons:
- Log In button (CSS: button.slds-button, text: Log In)

Links:
- Forgot Password link (text: Forgot your password?)

Locator Strategy:
- Use CSS selectors or XPath (IDs may be dynamic in Salesforce)
- Common pattern: input[name='fieldname']
- Buttons: button.slds-button or button[contains(text(), 'Log In')]

Test Scenarios:
1. Valid login with correct credentials
2. Invalid login with wrong credentials
3. Empty username field validation
4. Empty password field validation
5. Forgot password link functionality
```

**Copy this** → Paste in **"🤖 Generate Tests"** tab → Generate!

---

## 🔍 How to Find Salesforce Element Locators

### **Step 1: Open Browser DevTools**

1. Open your Salesforce login page
2. Press **F12** (or Right-click → Inspect)
3. Click the **selector tool** (top-left icon in DevTools)

### **Step 2: Inspect Each Element**

**For Username Field:**
```
1. Click on the username field
2. DevTools highlights the HTML
3. Look for attributes:
   - name="username"
   - placeholder="Username"
   - class="slds-input"
```

**For Password Field:**
```
1. Click on the password field
2. Look for:
   - name="password"
   - type="password"
   - class="slds-input"
```

**For Login Button:**
```
1. Click on the button
2. Look for:
   - class="slds-button"
   - Text content: "Log In"
```

### **Step 3: Test Locators in Console**

Open Console tab in DevTools and test:

```javascript
// Test username field
document.querySelector('input[name="username"]')

// Test password field
document.querySelector('input[type="password"]')

// Test login button
document.querySelector('button.slds-button')

// If these return the element, the locator works!
```

---

## 🛠️ Selenium Test Code for Salesforce

### **Using CSS Selectors (Recommended)**

```csharp
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

public class SalesforcePARSLoginTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private const string BASE_URL = "https://hrsa-dcpaas--dcpuat.sandbox.my.site.com/pars/s/login/";

    public SalesforcePARSLoginTests()
    {
        var options = new ChromeOptions();
        // Uncomment for headless mode:
        // options.AddArgument("--headless");
        
        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        _driver.Manage().Window.Maximize();
    }

    [Fact]
    public void Test_Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        _driver.Navigate().GoToUrl(BASE_URL);
        
        // Wait for page to load
        _wait.Until(d => d.FindElement(By.CssSelector("input[name='username']")));
        
        // Act
        var usernameField = _driver.FindElement(By.CssSelector("input[name='username']"));
        var passwordField = _driver.FindElement(By.CssSelector("input[type='password']"));
        var loginButton = _driver.FindElement(By.CssSelector("button.slds-button"));
        
        usernameField.SendKeys("your_username");
        passwordField.SendKeys("your_password");
        loginButton.Click();
        
        // Assert
        _wait.Until(d => d.Url != BASE_URL); // Wait for redirect
        Assert.NotEqual(BASE_URL, _driver.Url);
    }

    [Fact]
    public void Test_Login_WithInvalidCredentials_ShouldShowError()
    {
        // Arrange
        _driver.Navigate().GoToUrl(BASE_URL);
        _wait.Until(d => d.FindElement(By.CssSelector("input[name='username']")));
        
        // Act
        _driver.FindElement(By.CssSelector("input[name='username']")).SendKeys("invalid_user");
        _driver.FindElement(By.CssSelector("input[type='password']")).SendKeys("wrong_password");
        _driver.FindElement(By.CssSelector("button.slds-button")).Click();
        
        // Assert - Look for error message
        _wait.Until(d => d.FindElements(By.CssSelector(".slds-form-element__help, .error-message")).Count > 0);
        var errorElement = _driver.FindElement(By.CssSelector(".slds-form-element__help, .error-message"));
        Assert.NotNull(errorElement);
    }

    [Fact]
    public void Test_ForgotPasswordLink_ShouldBePresent()
    {
        // Arrange
        _driver.Navigate().GoToUrl(BASE_URL);
        
        // Act & Assert
        var forgotPasswordLink = _wait.Until(d => 
            d.FindElement(By.XPath("//a[contains(text(), 'Forgot')]"))
        );
        Assert.True(forgotPasswordLink.Displayed);
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
```

---

## 💡 Common Salesforce Locator Patterns

### **Input Fields**
```csharp
// By name attribute
By.CssSelector("input[name='username']")

// By placeholder
By.CssSelector("input[placeholder='Username']")

// By SLDS class
By.CssSelector("input.slds-input")

// By type
By.CssSelector("input[type='password']")
```

### **Buttons**
```csharp
// By SLDS class
By.CssSelector("button.slds-button")

// By text content
By.XPath("//button[contains(text(), 'Log In')]")

// By title attribute
By.CssSelector("button[title='Log In']")
```

### **Links**
```csharp
// By text
By.LinkText("Forgot your password?")

// Partial text
By.PartialLinkText("Forgot")

// XPath
By.XPath("//a[contains(text(), 'Forgot')]")
```

### **Error Messages**
```csharp
// SLDS error class
By.CssSelector(".slds-form-element__help")

// Generic error
By.CssSelector("[role='alert']")

// By text
By.XPath("//*[contains(text(), 'error') or contains(text(), 'invalid')]")
```

---

## 🚀 Quick Start for Your Specific Page

### **1. Create Test Project**
```powershell
dotnet new xunit -n SalesforcePARSTests
cd SalesforcePARSTests
dotnet add package Selenium.WebDriver
dotnet add package Selenium.WebDriver.ChromeDriver
dotnet add package Selenium.Support
```

### **2. Create Test File**
Create `LoginTests.cs` and paste the code above

### **3. Update Credentials**
Replace `"your_username"` and `"your_password"` with test credentials

### **4. Run Tests**
```powershell
dotnet test
```

---

## 🔧 Troubleshooting Salesforce Tests

### **Issue: Elements Not Found**
```csharp
// Add longer waits for Salesforce
_wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

// Wait for specific element
_wait.Until(d => d.FindElement(By.CssSelector("input[name='username']")));

// Wait for page to be ready
_wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
```

### **Issue: Shadow DOM Elements**
```csharp
// If element is in Shadow DOM, use JavaScript
var shadowHost = _driver.FindElement(By.CssSelector("your-shadow-host"));
var shadowRoot = (IWebElement)((IJavaScriptExecutor)_driver)
    .ExecuteScript("return arguments[0].shadowRoot", shadowHost);
var element = shadowRoot.FindElement(By.CssSelector("input"));
```

### **Issue: Dynamic IDs**
```csharp
// Don't use IDs in Salesforce - they change!
// ❌ Bad: By.Id("input-123")
// ✅ Good: By.CssSelector("input[name='username']")
```

---

## 📊 Best Practices for Salesforce Testing

1. **Use CSS Selectors or XPath** - Avoid IDs (they're dynamic)
2. **Add Generous Waits** - Salesforce pages load slowly
3. **Wait for Specific Elements** - Don't rely on implicit waits
4. **Handle iFrames** - Some Salesforce pages use iframes
5. **Test in Incognito** - Avoid cached sessions
6. **Use Explicit Waits** - Better than Thread.Sleep()

---

## 🎯 For Your Immediate Need

**Right now, use this manual description in the Test Generator:**

```
Salesforce PARS Login

Input Fields:
- Username (CSS: input[name='username'], placeholder: Username)
- Password (CSS: input[type='password'], placeholder: Password)

Buttons:
- Log In (CSS: button.slds-button)

Links:
- Forgot Password (XPath: //a[contains(text(), 'Forgot')])
```

Then generate tests and adjust the locators based on what you find in DevTools!

---

**Need help finding specific locators? Inspect the element and share the HTML, I'll help you create the right selector!** 🚀
