using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.TargetChildID
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class TargetChildIDTests : IClassFixture<AppConfig>
    {
        private const string TargetChildLinkSelector =
            "a.list-group-item.moreInfo[href*='TCIDs.aspx'], " +
            "a.moreInfo[data-formtype='TCIBO'], " +
            "a.list-group-item[title*='Target Child Information']";
        private const string InfoAlertSelector = "div.alert.alert-info";

        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public TargetChildIDTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(1)]
        public void NavigateToTargetChildInformationForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void NewTcidButtonDisplaysInfoAlert(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);

            var newTcidButton = driver.FindElements(By.CssSelector("div.panel-heading a.btn.btn-default.pull-right, div.panel-heading a.btn.btn-default"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("New TCID", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("New TCID button was not found on the Target Child Information page.");

            _output.WriteLine("[INFO] Clicking New TCID button.");
            CommonTestHelper.ClickElement(driver, newTcidButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            var infoAlert = driver.WaitforElementToBeInDOM(By.CssSelector(InfoAlertSelector), 10)
                ?? throw new InvalidOperationException("Info alert did not appear after opening New TCID form.");

            var alertText = infoAlert.Text?.Trim() ?? string.Empty;
            _output.WriteLine($"[INFO] Info alert text: {alertText}");
            Assert.Contains("Complete this form upon the birth of the target child", alertText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("target child is the newborn", alertText, StringComparison.OrdinalIgnoreCase);

            var validationSummary = driver.FindElements(By.CssSelector("div[id$='ValidationSummary1'], div.validation-summary"))
                .FirstOrDefault();
            Assert.NotNull(validationSummary);
            _output.WriteLine("[PASS] Validation summary container located on the New TCID form.");
        }

        private void NavigateToTargetChildPage(IPookieWebDriver driver, IWebElement formsPane, string pc1Id)
        {
            var targetChildLink = formsPane.FindElements(By.CssSelector(TargetChildLinkSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Target Child Information and Birth Outcomes link was not found in the Forms tab.");

            _output.WriteLine($"[INFO] Clicking link: {targetChildLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, targetChildLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url ?? string.Empty;
            _output.WriteLine($"[INFO] Current URL: {currentUrl}");

            Assert.Contains("TCIDs.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"pc1id={pc1Id}", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Target Child Information page opened with expected PC1 query string.");
        }
    }
}

