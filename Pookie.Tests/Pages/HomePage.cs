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

        public GridsPage GotoGridsPage()
        {
            _driver.WaitForUpdatePanel();
            var nav = _driver.FindElement(By.CssSelector("#mainNav"));
            var link = nav.FindElement(By.LinkText("Grids"));
            link.Click();
            _driver.WaitForReady();
            return new GridsPage(_driver);
        }

        public JSPage GotoJSPage()
        {
            _driver.WaitForUpdatePanel();
            var nav = _driver.FindElement(By.CssSelector("#mainNav"));
            var link = nav.FindElement(By.LinkText("JS"));
            link.Click();
            _driver.WaitForReady();
            return new JSPage(_driver);
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

        public bool SetTextBox(string value)
        {
            _driver.WaitForReady();
            var txtbox = _driver.GetElementByIDDollarSign("txtName");
            txtbox.SetValue(value);
            _driver.WaitForUpdatePanel();
            return txtbox.GetElementValue() != String.Empty;
        }

        public void ClickHTMLButton()
        {
            _driver.WaitForReady();
            var button = _driver.GetElementByIDDollarSign("btnNormal");
            button.Click();
            _driver.WaitForReady();
        }
    }
}