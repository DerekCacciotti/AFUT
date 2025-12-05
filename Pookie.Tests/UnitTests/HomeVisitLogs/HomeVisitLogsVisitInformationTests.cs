using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests.Attributes;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.HomeVisitLogs
{
    public class HomeVisitLogsVisitInformationTests : HomeVisitLogsTestBase
    {
        public HomeVisitLogsVisitInformationTests(AppConfig config, ITestOutputHelper output)
            : base(config, output)
        {
        }

        [Theory]
        [MemberData(nameof(HomeVisitLogsTestBase.GetTestPc1Ids), MemberType = typeof(HomeVisitLogsTestBase))]
        [TestPriority(3)]
        public void VisitInformationTabValidationsReactToUserInput(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            _output.WriteLine($"[INFO] Starting Visit Information tab validation test for PC1 {pc1Id}.");
            NavigateToHomeVisitLogs(driver, pc1Id);
            OpenNewHomeVisitLog(driver);

            SetDateOfVisit(driver, "11/16/25");
            driver.WaitForUpdatePanel(15);
            driver.WaitForReady(15);
            Thread.Sleep(300);

            _output.WriteLine("[DEBUG] Submitting date to load Visit Information tab.");
            ClickSubmit(driver); // submit to load the full Visit Information form
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            var visitInfoContainer = driver.WaitforElementToBeInDOM(By.CssSelector("div#main-div"), 30)
                ?? throw new InvalidOperationException("Visit Information form container did not appear after submitting the date.");
            _output.WriteLine($"[DEBUG] Visit Information container detected with text length {visitInfoContainer.Text?.Length ?? 0}.");

            _output.WriteLine("[DEBUG] Clicking Save Partial to trigger Visit Information validations.");
            ClickSubmit(driver); // Save partial after form is loaded
            var validationText = GetValidationMessages(driver);
            _output.WriteLine($"[DEBUG] Validation text after initial submit: '{validationText}'");
            AssertValidationContains(validationText,
                "Missing Start Time of Visit.",
                "Missing AM/PM of Visit.",
                "Missing type of visit",
                "Missing total length of visit",
                "[Visit Information tab] Total Length of Visit must be at least 30 minutes!",
                "Who participated in this home visit - Missing participants");

            SetVisitStartTime(driver, "0800");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            _output.WriteLine($"[DEBUG] Validation text after entering start time only: '{validationText}'");
            Assert.DoesNotContain("Missing Start Time of Visit.", validationText, StringComparison.OrdinalIgnoreCase);

            if (validationText.Contains("Missing AM/PM of Visit.", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine("[DEBUG] AM/PM validation still present, selecting AM explicitly.");
                SelectVisitStartPeriod(driver, "AM", "1");
                ClickSubmit(driver);
                validationText = GetValidationMessages(driver);
                _output.WriteLine($"[DEBUG] Validation text after selecting AM/PM: '{validationText}'");
                Assert.DoesNotContain("Missing AM/PM of Visit.", validationText, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                _output.WriteLine("[INFO] AM/PM defaulted automatically; skipping AM selection.");
            }

            Assert.Contains("Missing type of visit", validationText, StringComparison.OrdinalIgnoreCase);

            SelectVisitTypeOption(driver, 3);
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            _output.WriteLine($"[DEBUG] Validation text after selecting type of visit: '{validationText}'");
            Assert.DoesNotContain("Missing type of visit", validationText, StringComparison.OrdinalIgnoreCase);

            SelectFromChosen(driver,
                "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlVisitWhere_chosen",
                "6. Other (specify)");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(300);

            var otherWhereSpecify = driver.WaitforElementToBeInDOM(By.CssSelector("div[id$='divOtherwhereSpecify']"), 10)
                ?? throw new InvalidOperationException("Other location specify container was not found.");
            Assert.True(otherWhereSpecify.Displayed, "Other location specify container should be visible when 'Other (specify)' is selected.");
            var otherWhereInput = otherWhereSpecify.FindElement(By.CssSelector("input[id$='txtOtherwhereSpecify']"));
            WebElementHelper.SetInputValue(driver, otherWhereInput, "Community center", "Other location specify", triggerBlur: true);

            SelectFromChosen(driver,
                "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlVisitLocationWhy_chosen",
                "11. Other (specify)");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(300);

            var otherWhySpecify = driver.WaitforElementToBeInDOM(By.CssSelector("div[id$='divOtherwhySpecify']"), 10)
                ?? throw new InvalidOperationException("Other location reason specify container was not found.");
            Assert.True(otherWhySpecify.Displayed, "Other location reason specify container should be visible when 'Other (specify)' is selected.");
            var otherWhyInput = otherWhySpecify.FindElement(By.CssSelector("input[id$='txtOtherwhySpecify']"));
            WebElementHelper.SetInputValue(driver, otherWhyInput, "Construction at home", "Other why specify", triggerBlur: true);

            // Reset dropdowns to default before switching visit type
            SelectFromChosen(driver,
                "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlVisitWhere_chosen",
                "--Select--");
            SelectFromChosen(driver,
                "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlVisitLocationWhy_chosen",
                "--Select--");

            // Reset visit type to option 0 (inside home) and verify location validations clear to total length + participants
            UnselectVisitTypeOption(driver, 3);
            SelectVisitTypeOption(driver, 0);
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            _output.WriteLine($"[DEBUG] Validation text after deselecting outside visit: '{validationText}'");
            Assert.DoesNotContain("You must select where the visit took place", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("You must select the reason why the home visit was outside the home", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Missing total length of visit", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[Visit Information tab] Total Length of Visit must be at least 30 minutes!", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Who participated in this home visit - Missing participants", validationText, StringComparison.OrdinalIgnoreCase);

            // Enter visit length minutes < 30 and expect minimum duration validation to persist
            SetVisitLengthMinutes(driver, "29");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            _output.WriteLine($"[DEBUG] Validation text after entering 29 minutes: '{validationText}'");
            Assert.DoesNotContain("Missing total length of visit", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[Visit Information tab] Total Length of Visit must be at least 30 minutes!", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Who participated in this home visit - Missing participants", validationText, StringComparison.OrdinalIgnoreCase);

            // Enter visit length minutes >= 30 and expect only participants validation
            SetVisitLengthMinutes(driver, "45");
            ClickSubmit(driver);
            validationText = GetValidationMessages(driver);
            _output.WriteLine($"[DEBUG] Validation text after entering 45 minutes: '{validationText}'");
            Assert.DoesNotContain("[Visit Information tab] Total Length of Visit must be at least 30 minutes!", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Who participated in this home visit - Missing participants", validationText, StringComparison.OrdinalIgnoreCase);

            // Verify participants section dynamic behavior
            var supervisorCheckbox = driver.FindElement(By.CssSelector("input[id$='chkHVSupervisorParticipated']"));
            supervisorCheckbox.Click();
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            var supervisorObservation = driver.WaitforElementToBeInDOM(By.CssSelector("div#div-chkSupervisorObservation"), 10)
                ?? throw new InvalidOperationException("Supervisor observation section was not found after selecting HV Supervisor.");
            Assert.True(supervisorObservation.Displayed, "Supervisor observation checkbox should be visible when supervisor participates.");

            var nonPrimaryCheckbox = driver.FindElement(By.CssSelector("input[id$='chkNonPrimaryFSWParticipated']"));
            nonPrimaryCheckbox.Click();
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            var nonPrimaryDropdownContainer = driver.WaitforElementToBeInDOM(By.CssSelector("div#divNonPrimaryFSWFK"), 10)
                ?? throw new InvalidOperationException("Non-primary worker dropdown container was not found.");
            Assert.True(nonPrimaryDropdownContainer.Displayed, "Non-primary worker dropdown should be visible when FSS participates.");

            var nonPrimaryDropdown = nonPrimaryDropdownContainer.FindElement(By.CssSelector("select[id$='ddlNonPrimaryFSWFK']"));
            WebElementHelper.SelectDropdownOption(driver, nonPrimaryDropdown, "Non-primary worker dropdown", "105, Worker", "105");

            var otherParticipantCheckbox = driver.FindElement(By.CssSelector("input[id$='chkOtherParticipated']"));
            otherParticipantCheckbox.Click();
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            var otherSpecifyContainer = driver.WaitforElementToBeInDOM(By.CssSelector("div#divParticipatedSpecify"), 10)
                ?? throw new InvalidOperationException("Other participant specify container was not found.");
            Assert.True(otherSpecifyContainer.Displayed, "Other participant specify input should be visible when 'Other' is selected.");
            var otherSpecifyInput = otherSpecifyContainer.FindElement(By.CssSelector("input[id$='txtParticipatedSpecify']"));
            WebElementHelper.SetInputValue(driver, otherSpecifyInput, "Family Friend", "Other participant", triggerBlur: true);

            // Select at least one basic participant to satisfy validation
            var primaryCaregiverCheckbox = driver.FindElement(By.CssSelector("input[id$='chkPC1Participated']"));
            if (!primaryCaregiverCheckbox.Selected)
            {
                primaryCaregiverCheckbox.Click();
            }

            // Click Save Partial and verify toast appears
            _output.WriteLine("[DEBUG] Saving partial to verify success toast.");
            ClickSubmit(driver);
            driver.WaitForReady(10);
            Thread.Sleep(500);
            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            _output.WriteLine($"[DEBUG] Toast message: '{toastMessage}'");
            Assert.Contains("Partially Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Partially saved", toastMessage, StringComparison.OrdinalIgnoreCase);

            _output.WriteLine("[PASS] Visit Information tab validation flow completed successfully.");
        }
    }
}

