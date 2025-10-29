using AFUT.Tests.Driver;
using OpenQA.Selenium;
using System;

namespace AFUT.Tests.Pages
{
    [Page]
    public class HomePage : IAppLandingPage
    {
        private readonly IPookieWebDriver _driver;
        private readonly IWebElement? _legacyButtonContainer;
        private readonly IWebElement? _dashboardsPanel;
        private readonly IWebElement? _impersonateSection;
        private readonly IWebElement _usernameField;

        public bool ButtonClicked { get; private set; }

        public bool IsLoaded => _usernameField is not null;

        public HomePage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.WaitForReady(60);

            _usernameField = _driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='hfUsername']"), 60)
                ?? throw new InvalidOperationException("Home page hidden username field not found.");

            _driver.TryGetElement(By.CssSelector("#divButton"), out _legacyButtonContainer);
            _driver.TryGetElement(By.Id("ctl00_ContentPlaceHolder1_pnlDashboards"), out _dashboardsPanel);
            _driver.TryGetElement(By.Id("ctl00_ContentPlaceHolder1_divImpersonateWorker"), out _impersonateSection);
        }

        public GridsPage GotoGridsPage()
        {
            _driver.WaitForUpdatePanel();
            var link = GetNavigation().FindElement(By.LinkText("Grids"));
            link.Click();
            _driver.WaitForReady();
            return new GridsPage(_driver);
        }

        public JSPage GotoJSPage()
        {
            _driver.WaitForUpdatePanel();
            var link = GetNavigation().FindElement(By.LinkText("JS"));
            link.Click();
            _driver.WaitForReady();
            return new JSPage(_driver);
        }

        public void ClickButton()
        {
            _driver.WaitForReady();
            if (_legacyButtonContainer is null)
            {
                throw new InvalidOperationException("Legacy button container is not available on this home page.");
            }

            var button = _legacyButtonContainer.FindElement(By.CssSelector("#MainContent_btnTest"));
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

        private IWebElement GetNavigation()
        {
            return _driver.FindElement(By.CssSelector(".navbar"));
        }
    }
}