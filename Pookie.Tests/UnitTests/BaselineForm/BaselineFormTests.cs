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
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.BaselineForm
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class BaselineFormTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public BaselineFormTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(1)]
        public void BaselineFormLinkNavigatesToIntakePage(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            var baselineFormLink = formsPane.FindElements(By.CssSelector(
                    "a.list-group-item.moreInfo[href*='Intake.aspx'], " +
                    "a.moreInfo[data-formtype='in'], " +
                    "a.list-group-item[title='Intake']"))
                .FirstOrDefault(el => el.Displayed &&
                                      (el.Text?.Contains("Baseline", StringComparison.OrdinalIgnoreCase) ?? false))
                ?? throw new InvalidOperationException("Baseline Form link was not found in the Forms tab.");

            _output.WriteLine($"[INFO] Found Baseline Form link: {baselineFormLink.Text?.Trim()}");

            CommonTestHelper.ClickElement(driver, baselineFormLink);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url ?? string.Empty;
            _output.WriteLine($"[INFO] Baseline Form navigated to: {currentUrl}");

            Assert.StartsWith("https://hfnytesting.azurewebsites.net/Pages/Intake.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"pc1id={pc1Id}", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ipk=57561", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Intake page opened with expected parameters for Baseline Form");
        }
    }
}


