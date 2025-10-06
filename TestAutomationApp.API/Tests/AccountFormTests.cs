using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;
using System;

namespace AutomatedTests
{
    public class AccountFormTests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private const string BaseUrl = "https://your-application-url.com";

        public AccountFormTests()
        {
            _driver = new ChromeDriver();
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _driver.Manage().Window.Maximize();
        }

        [Fact]
        public void Test_CreateNewAccount_WithValidData_ShouldSucceed()
        {
            // Arrange
            _driver.Navigate().GoToUrl(BaseUrl);
            
            // Act - Fill Employee Information
            var salutationDropdown = new SelectElement(_driver.FindElement(By.Id("salutation")));
            salutationDropdown.SelectByValue("Mr.");
            
            _driver.FindElement(By.Id("firstName")).SendKeys("John");
            _driver.FindElement(By.Id("middleInitial")).SendKeys("A");
            _driver.FindElement(By.Id("lastName")).SendKeys("Doe");
            _driver.FindElement(By.Id("employeeId")).SendKeys("EMP001");
            
            // Fill Contact Address
            _driver.FindElement(By.Id("emailAddress")).SendKeys("john.doe@example.com");
            _driver.FindElement(By.Id("phoneNumber")).SendKeys("555-1234");
            _driver.FindElement(By.Id("faxNumber")).SendKeys("555-5678");
            
            // Fill Employee Details
            _driver.FindElement(By.Id("orgType-hhs")).Click();
            _driver.FindElement(By.Id("coordinatorName")).SendKeys("Jane Smith");
            _driver.FindElement(By.Id("status-temporary")).Click();
            _driver.FindElement(By.Id("workLocation-withUS")).Click();
            
            var officeBureauDropdown = new SelectElement(_driver.FindElement(By.Id("officeBureau")));
            officeBureauDropdown.SelectByIndex(1);
            
            _driver.FindElement(By.Id("officeSearchList")).SendKeys("Office details here");
            
            // Submit form
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            
            // Assert
            _wait.Until(d => d.FindElement(By.CssSelector(".success-message")));
            var successMessage = _driver.FindElement(By.CssSelector(".success-message")).Text;
            Assert.Contains("successfully", successMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Test_CreateAccount_WithMissingRequiredFields_ShouldShowValidation()
        {
            // Arrange
            _driver.Navigate().GoToUrl(BaseUrl);
            
            // Act - Submit without filling required fields
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            
            // Assert - Check for validation messages
            var firstNameInput = _driver.FindElement(By.Id("firstName"));
            var validationMessage = firstNameInput.GetAttribute("validationMessage");
            Assert.False(string.IsNullOrEmpty(validationMessage), "Validation message should not be null or empty");
        }

        [Fact]
        public void Test_EmailField_WithInvalidFormat_ShouldShowValidation()
        {
            // Arrange
            _driver.Navigate().GoToUrl(BaseUrl);
            
            // Act
            var emailField = _driver.FindElement(By.Id("emailAddress"));
            emailField.SendKeys("invalid-email");
            emailField.SendKeys(Keys.Tab);
            
            // Assert
            var result = ((IJavaScriptExecutor)_driver)
                .ExecuteScript("return arguments[0].checkValidity();", emailField);
            var isValid = result != null && (bool)result;
            Assert.False(isValid);
        }

        [Fact]
        public void Test_RadioButtons_OrganizationType_ShouldSelectCorrectly()
        {
            // Arrange
            _driver.Navigate().GoToUrl(BaseUrl);
            
            // Act
            var contractorRadio = _driver.FindElement(By.Id("orgType-contractor"));
            contractorRadio.Click();
            
            // Assert
            Assert.True(contractorRadio.Selected);
        }

        [Fact]
        public void Test_FormFields_MaxLength_ShouldEnforce()
        {
            // Arrange
            _driver.Navigate().GoToUrl(BaseUrl);
            
            // Act
            var firstNameField = _driver.FindElement(By.Id("firstName"));
            var longString = new string('A', 50);
            firstNameField.SendKeys(longString);
            
            // Assert
            var actualValue = firstNameField.GetAttribute("value") ?? string.Empty;
            Assert.True(actualValue.Length <= 32, "First name should not exceed 32 characters");
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
