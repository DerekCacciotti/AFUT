using AFUT.Tests.Driver;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Pages
{
    [Page]
    public class HomePage
    {
        private readonly IPookieWebDriver _driver;
        private IWebElement element;

        public bool ButtonClicked { get; private set; }

        public HomePage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            element = driver.FindElement(By.CssSelector("#divButton"));
        }

        public void ClickButton()
        {
            _driver.WaitForReady();
            var button = element.FindElement(By.CssSelector("#MainContent_btnTest"));
            if (button != null)
            {
                button.Click();
                _driver.WaitForUpdatePanel();
                var label = _driver.GetElementByIDDollarSign("lblText");
                ButtonClicked = label.Text != String.Empty;
            }
        }

        public bool SetDropdownList(string value)
        {
            _driver.WaitForReady();
            var ddl = _driver.GetElementByIDDollarSign("ddlNames");
            ddl.SetValue(value);
            _driver.WaitForUpdatePanel();
            return ddl.GetElementValue() != String.Empty;
        }
    }
}