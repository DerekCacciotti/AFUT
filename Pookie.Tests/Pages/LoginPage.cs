using AFUT.Tests.Driver;
using OpenQA.Selenium;

namespace AFUT.Tests.Pages
{
    [Page]
    public class LoginPage
    {
        private readonly IPookieWebDriver driver;

        public LoginPage(IPookieWebDriver driver)
        {
            this.driver = driver;
        }

        public void SignIn(string userName, string password)
        {
            driver.WaitForReady();

            var userBox = driver.FindElement(By.Id("Login1_UserName"));
            var passwordBox = driver.FindElement(By.Id("Login1_Password"));
            var submitButton = driver.FindElement(By.Id("Login1_LoginButton"));

            userBox.Clear();
            userBox.SendKeys(userName);
            passwordBox.Clear();
            passwordBox.SendKeys(password);
            submitButton.Click();

            driver.WaitForReady(60);
        }

        public bool IsSignedIn(int timeoutSeconds = 60)
        {
            driver.WaitForReady(timeoutSeconds);

            if (driver.WaitForElementToLeaveDOM(By.Id("Login1_LoginButton"), timeoutSeconds))
            {
                return true;
            }

            var element = driver.WaitforElementToBeInDOM(By.CssSelector("#mainNav"), timeoutSeconds);

            return element?.Displayed == true;
        }
    }
}

