using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests.Attributes;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HomeVisitLogs
{
    public class HomeVisitLogsPciTabTests : HomeVisitLogsTestBase
    {
        public HomeVisitLogsPciTabTests(AppConfig config, ITestOutputHelper output)
            : base(config, output)
        {
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(9)]
        public void ParentChildInteractionTabDisplaysCoreSections(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting PCI tab test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var pciTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkpci"), 10)
                ?? throw new InvalidOperationException("PCI tab link was not found.");
            pciTabLink.Click();
            driver.WaitForReady(5);
            Thread.Sleep(300);

            var pciPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#pci"), 10)
                ?? throw new InvalidOperationException("PCI tab content was not found.");
            Assert.True(pciPane.Displayed, "PCI tab content was not displayed.");

            var header = pciPane.FindElements(By.CssSelector(".panel-heading .panel-title")).FirstOrDefault()
                         ?? pciPane.FindElements(By.CssSelector("h4")).FirstOrDefault();
            Assert.NotNull(header);
            Assert.Contains("Parent-Child Interaction", header.Text ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            var participationCheckbox = pciPane.FindElements(By.CssSelector("input[id$='chkPCChildInteraction']")).FirstOrDefault()
                ?? throw new InvalidOperationException("Parent-Child interaction checkbox was not found.");
            Assert.True(participationCheckbox.Enabled, "Parent-Child interaction checkbox should be enabled.");

            _output.WriteLine("[PASS] PCI tab header and core fields are displayed.");
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(10)]
        public void ParentChildInteractionCheckboxesRevealComments(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting PCI checkbox visibility test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenExistingHomeVisitLog(driver);

            var pciTabLink = driver.WaitforElementToBeInDOM(By.CssSelector("a#lnkpci"), 10)
                ?? throw new InvalidOperationException("PCI tab link was not found.");
            pciTabLink.Click();
            driver.WaitForReady(5);

            var pciPane = driver.WaitforElementToBeInDOM(By.CssSelector("div#pci"), 10)
                ?? throw new InvalidOperationException("PCI tab content was not found.");

            var commentContainer = pciPane.FindElement(By.CssSelector("div#divPCIComments"));
            var commentTextarea = commentContainer.FindElement(By.CssSelector("textarea[id$='txtPCComments']"));

            var checkboxes = pciPane.FindElements(By.CssSelector("span.PCI input[type='checkbox']"));
            Assert.NotEmpty(checkboxes);

            foreach (var checkbox in checkboxes)
            {
                checkbox.Click();
                driver.WaitForUpdatePanel(3);
                driver.WaitForReady(3);
                Thread.Sleep(150);

                Assert.True(commentContainer.Displayed, $"Comment section should be visible when '{checkbox.GetAttribute("id")}' is selected.");
                WebElementHelper.SetInputValue(driver, commentTextarea, $"Notes for {checkbox.GetAttribute("id")}", "PCI comments", triggerBlur: true);

                checkbox.Click();
                driver.WaitForUpdatePanel(3);
                driver.WaitForReady(3);
                Thread.Sleep(150);
                Assert.False(commentContainer.Displayed, $"Comment section should hide when '{checkbox.GetAttribute("id")}' is unselected.");
            }

            _output.WriteLine("[PASS] PCI checkboxes toggle the shared comments textarea.");
        }
    }
}

