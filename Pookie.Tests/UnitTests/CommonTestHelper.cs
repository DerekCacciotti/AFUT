using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests
{
    /// <summary>
    /// Common helper methods for test suites that handle navigation flows.
    /// </summary>
    public static class CommonTestHelper
    {
        /// <summary>
        /// Performs the common flow: Login -> DataEntry -> Search -> EC ID -> Forms Tab
        /// </summary>
        /// <param name="driver">The web driver instance</param>
        /// <param name="config">Application configuration</param>
        /// <param name="driverFactory">Driver factory for creating drivers</param>
        /// <param name="targetPc1Id">The PC1 ID to search for</param>
        /// <returns>The Forms tab pane element</returns>
        public static (HomePage homePage, IWebElement formsPane) NavigateToFormsTab(
            IPookieWebDriver driver,
            AppConfig config,
            string targetPc1Id)
        {
            // Step 1: Login
            driver.Navigate().GoToUrl(config.AppUrl);
            driver.WaitForReady(30);

            var loginPage = new LoginPage(driver);
            loginPage.SignIn(config.UserName, config.Password);

            Assert.True(loginPage.IsSignedIn(), "User was not signed in successfully.");

            // Step 2: Select DataEntry role
            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.IsType<HomePage>(landingPage);
            var homePage = (HomePage)landingPage;

            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            // Step 3: Navigate to Search Cases
            var navigationBar = driver.WaitforElementToBeInDOM(By.CssSelector(".navbar"), 30)
                ?? throw new InvalidOperationException("Navigation bar was not present on the page.");

            var searchCasesButton = navigationBar.WaitforElementToBeInDOM(By.CssSelector(".btn-group.middle a[href*='SearchCases.aspx']"), 10)
                ?? throw new InvalidOperationException("Search Cases button was not found.");

            searchCasesButton.Click();
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);

            var searchCasesPage = new SearchCasesPage(driver);
            Assert.True(searchCasesPage.IsLoaded, "Search Cases page did not load after clicking the shortcut.");

            // Step 4: Search for the EC ID
            var pc1Input = driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='txtPC1ID']"), 5)
                ?? throw new InvalidOperationException("PC1 ID input was not found on the Search Cases page.");

            pc1Input.Clear();
            pc1Input.SendKeys(targetPc1Id);

            var searchButton = driver.WaitforElementToBeInDOM(By.CssSelector("a#ctl00_ContentPlaceHolder1_btSearch"), 5)
                ?? throw new InvalidOperationException("Search button was not found on the Search Cases page.");

            searchButton.Click();
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);

            // Step 5: Click Forms tab
            var formsTab = driver.WaitforElementToBeInDOM(By.CssSelector("a#formstab[data-toggle='tab'][href='#forms']"), 10)
                ?? throw new InvalidOperationException("Forms tab was not found on the Search Cases results.");
            formsTab.Click();
            driver.WaitForReady(5);

            var formsPane = driver.WaitforElementToBeInDOM(By.CssSelector(".tab-pane#forms"), 5)
                ?? throw new InvalidOperationException("Forms tab content was not found.");
            
            if (!formsPane.Displayed || !formsPane.GetAttribute("class").Contains("active", StringComparison.OrdinalIgnoreCase))
            {
                formsTab.Click();
                driver.WaitForReady(3);
                formsPane = driver.WaitforElementToBeInDOM(By.CssSelector(".tab-pane#forms"), 5)
                    ?? throw new InvalidOperationException("Forms tab content was not found after activation.");
            }

            return (homePage, formsPane);
        }

        /// <summary>
        /// Finds and returns the PC1 ID display text from the page
        /// </summary>
        public static string? FindPc1Display(IPookieWebDriver driver, string targetPc1Id)
        {
            var pc1IdDisplay = driver.FindElements(By.CssSelector("[id$='lblPC1ID'], [id$='lblPc1Id'], .pc1-id, .pc1-id-value"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(pc1IdDisplay))
            {
                return pc1IdDisplay;
            }

            return driver.FindElements(By.CssSelector(".panel-body, .card-body, .form-group, .list-group, .list-group-item, .row"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text) && el.Text.Contains(targetPc1Id, StringComparison.OrdinalIgnoreCase))
                .Select(el => el.Text.Trim())
                .FirstOrDefault();
        }

        /// <summary>
        /// Clicks an element, using JavaScript as fallback if needed
        /// </summary>
        public static void ClickElement(IPookieWebDriver driver, IWebElement element)
        {
            try
            {
                element.Click();
                return;
            }
            catch (Exception)
            {
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                System.Threading.Thread.Sleep(200);
                js.ExecuteScript("arguments[0].click();", element);
            }
        }

        /// <summary>
        /// Performs the common flow: Login -> DataEntry -> Reports HomePage
        /// </summary>
        /// <param name="driver">The web driver instance</param>
        /// <param name="config">Application configuration</param>
        /// <param name="output">Optional test output helper for logging</param>
        /// <returns>The HomePage instance after navigation</returns>
        public static Pages.HomePage NavigateToReportsHomePage(
            IPookieWebDriver driver,
            AppConfig config,
            ITestOutputHelper? output = null)
        {
            // Step 1: Login
            driver.Navigate().GoToUrl(config.AppUrl);
            driver.WaitForReady(30);

            var loginPage = new LoginPage(driver);
            loginPage.SignIn(config.UserName, config.Password);

            Assert.True(loginPage.IsSignedIn(), "User was not signed in successfully.");

            // Step 2: Select DataEntry role
            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.IsType<Pages.HomePage>(landingPage);
            var homePage = (Pages.HomePage)landingPage;

            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            // Step 3: Navigate to Reports
            var navigationBar = driver.WaitforElementToBeInDOM(By.CssSelector(".navbar"), 30)
                ?? throw new InvalidOperationException("Navigation bar was not present on the page.");

            var reportsButton = navigationBar.FindElements(By.CssSelector(
                "a#lnkReportCatalog, " +
                "a[href*='ReportCatalog.aspx'], " +
                "a[href*='/Reports/']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Reports button was not found in the navigation bar.");

            ClickElement(driver, reportsButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            System.Threading.Thread.Sleep(1000);

            // Verify we're on the Reports page
            var currentUrl = driver.Url;
            var isOnReportsPage = currentUrl.Contains("ReportCatalog.aspx", StringComparison.OrdinalIgnoreCase) ||
                                   currentUrl.Contains("/Reports/", StringComparison.OrdinalIgnoreCase) ||
                                   currentUrl.Contains("Reports.aspx", StringComparison.OrdinalIgnoreCase);
            
            Assert.True(isOnReportsPage, $"Expected to be on Reports page, but current URL is: {currentUrl}");

            return homePage;
        }
    }
}

