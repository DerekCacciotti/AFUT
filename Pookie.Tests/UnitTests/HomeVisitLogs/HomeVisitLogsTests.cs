using System;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.UnitTests.Attributes;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HomeVisitLogs
{
    public class HomeVisitLogsTests : HomeVisitLogsTestBase
    {
        public HomeVisitLogsTests(AppConfig config, ITestOutputHelper output)
            : base(config, output)
        {
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(1)]
        public void HomeVisitLogsLinkOpensForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting Home Visit Logs navigation test for PC1 {pc1Id}.");

            NavigateToHomeVisitLogs(driver, pc1Id);

            var currentUrl = driver.Url ?? string.Empty;
            _output.WriteLine($"[INFO] Current URL after click: {currentUrl}");

            Assert.Contains("HomeVisitLogs.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine("[PASS] Home Visit Logs URL loaded successfully.");
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(2)]
        public void HomeVisitLogsValidatesDateOfVisitField(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting Date of Visit validation test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);

            OpenNewHomeVisitLog(driver);

            ClickSubmit(driver);
            var validationText = GetValidationMessages(driver);
            Assert.Contains("Missing Date of Visit.", validationText, StringComparison.OrdinalIgnoreCase);

            SetDateOfVisit(driver, "11/16/16");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            AssertValidationContains(validationText,
                "Not a valid Date of Visit, Date of Visit must not be prior to the Case Start Date.",
                "Not a valid Date of Visit, Date of Visit must not be prior to the Intake Date.",
                "Not a valid Date of Visit, must be after 9/2/2017");

            SetDateOfVisit(driver, "11/11/18");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            AssertValidationContains(validationText,
                "Not a valid Date of Visit, Date of Visit must not be prior to the Case Start Date.",
                "Not a valid Date of Visit, Date of Visit must not be prior to the Intake Date.");

            const string validDate = "11/16/25";
            SetDateOfVisit(driver, validDate);
            ClickSubmit(driver);
            driver.WaitForUpdatePanel(45);
            driver.WaitForReady(45);
            Thread.Sleep(1500);

            var summaryContainer = driver.FindElements(By.CssSelector("#main-div .noprint, div.noprint"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text) &&
                                       el.Text.IndexOf("PC1ID:", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? throw new InvalidOperationException("Confirmation summary with PC1 information was not displayed after submitting a valid Home Visit Log.");

            var summaryText = summaryContainer.Text?.Trim() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(summaryText), "Confirmation summary was empty.");
            Assert.Contains("PC1ID:", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Date of Visit:", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(validDate, summaryText, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine($"[PASS] Date of Visit validation flow completed. Summary text: {summaryText}");
        }
    }
}

