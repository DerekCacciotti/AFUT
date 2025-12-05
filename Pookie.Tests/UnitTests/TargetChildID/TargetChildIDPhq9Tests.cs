using System;
using System.Globalization;
using System.Threading;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests.Attributes;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace AFUT.Tests.UnitTests.TargetChildID
{
    public partial class TargetChildIDTests
    {
        private const string DateFormatShortYear = "MM/dd/yy";
        private static readonly string[] AcceptedDateFormats = { "MM/dd/yy", "MM/dd/yyyy" };

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(8)]
        public void Phq9DateValidationAndSave(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenExistingTcidEntry(driver);

            const string phqTabTitle = "PHQ-9";
            SwitchToTab(driver, PhqTabSelector, phqTabTitle);

            var phqInput = driver.WaitforElementToBeInDOM(By.CssSelector(PhqDateInputSelector), 10)
                ?? throw new InvalidOperationException("PHQ date input was not found.");

            _output.WriteLine("[INFO] Entering invalid PHQ date value '000000'.");
            phqInput.Clear();
            phqInput.SendKeys("000000");
            phqInput.SendKeys(Keys.Enter);
            driver.WaitForReady(2);
            Thread.Sleep(200);
            var valueAfterInvalid = phqInput.GetAttribute("value")?.Trim() ?? string.Empty;
            Assert.True(string.IsNullOrEmpty(valueAfterInvalid), "PHQ date input should clear invalid entry '000000'.");

            var intakeLabel = driver.WaitforElementToBeInDOM(By.CssSelector(IntakeDateLabelSelector), 10)
                ?? throw new InvalidOperationException("Intake date label was not found.");
            var intakeDateText = intakeLabel.Text?.Trim() ?? string.Empty;
            var intakeDate = ParseDate(intakeDateText, "Intake date label");
            var dobLabel = driver.WaitforElementToBeInDOM(By.CssSelector(TargetChildDobLabelSelector), 10)
                ?? throw new InvalidOperationException("Target child DOB label was not found.");
            var dobText = dobLabel.Text?.Trim() ?? string.Empty;
            var targetChildDob = ParseDate(dobText, "Target child DOB label");

            var invalidPhqDate = intakeDate.AddDays(-1);
            SetPhqDate(driver, phqInput, invalidPhqDate);
            var summaryText = SubmitForm(driver);
            Assert.Contains("[PHQ-9] The PHQ date administered must be on or after the TC's date of birth!", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Validation message appeared for PHQ date prior to intake.");

            SwitchToTab(driver, PhqTabSelector, phqTabTitle);
            phqInput = driver.WaitforElementToBeInDOM(By.CssSelector(PhqDateInputSelector), 10)
                ?? throw new InvalidOperationException("PHQ date input was not found after validation.");

            var validPhqDate = targetChildDob.AddDays(1);
            SetPhqDate(driver, phqInput, validPhqDate);

            _output.WriteLine("[INFO] Selecting 'Other' for PHQ participant to verify specify input.");
            var participantDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(PhqParticipantDropdownSelector), 10)
                ?? throw new InvalidOperationException("PHQ participant dropdown was not found.");
            var participantSelect = new SelectElement(participantDropdown);
            participantSelect.SelectByValue("04");

            var participantSpecifyInput = driver.WaitforElementToBeInDOM(By.CssSelector(PhqParticipantSpecifyInputSelector), 10)
                ?? throw new InvalidOperationException("PHQ participant specify input was not found.");
            Assert.True(participantSpecifyInput.Displayed, "PHQ participant specify input should be visible when 'Other' is selected.");
            WebElementHelper.SetInputValue(driver, participantSpecifyInput, "Non-family support", "PHQ participant specify input", triggerBlur: true);

            participantSelect.SelectByValue("01");
            Assert.False(participantSpecifyInput.Displayed, "PHQ participant specify input should be hidden when not selecting 'Other'.");

            participantSelect.SelectByValue("02");
            var phqWorkerDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(PhqWorkerDropdownSelector), 10)
                ?? throw new InvalidOperationException("PHQ worker dropdown was not found.");
            var workerSelect = new SelectElement(phqWorkerDropdown);
            workerSelect.SelectByValue("105");

            _output.WriteLine("[INFO] Verifying PHQ totals remain N/A/Invalid when any question is left unselected.");
            SetPhqScores(driver, new[] { "01", "02", "01", "", "01", "02", "", "02", "01" });
            EnsureDifficultyAndReferralCleared(driver);
            driver.WaitForReady(1);
            Thread.Sleep(300);
            WaitForLabelText(driver, PhqResultLabelSelector, text => string.Equals(text, "N/A", StringComparison.OrdinalIgnoreCase));
            WaitForLabelText(driver, PhqScoreValidityLabelSelector, text => text.Contains("Invalid", StringComparison.OrdinalIgnoreCase));

            var difficultyDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(PhqDifficultyDropdownSelector), 10)
                ?? throw new InvalidOperationException("PHQ difficulty dropdown was not found.");
            var difficultySelect = new SelectElement(difficultyDropdown);
            difficultySelect.SelectByValue("02");

            var referralCheckbox = driver.WaitforElementToBeInDOM(By.CssSelector(PhqDepressionReferralCheckboxSelector), 10)
                ?? throw new InvalidOperationException("PHQ referral checkbox was not found.");
            if (!referralCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, referralCheckbox);
                driver.WaitForReady(1);
                Thread.Sleep(200);
            }

            _output.WriteLine("[INFO] Filling PHQ questions so total exceeds 9 to ensure Positive/Valid.");
            SetPhqScores(driver, new[] { "04", "04", "04", "04", "04", "04", "04", "04", "04" });
            driver.WaitForReady(1);
            Thread.Sleep(300);
            WaitForLabelText(driver, PhqResultLabelSelector, text => text.Contains("Positive", StringComparison.OrdinalIgnoreCase));
            WaitForLabelText(driver, PhqScoreValidityLabelSelector, text => text.Contains("Valid", StringComparison.OrdinalIgnoreCase));

            SubmitForm(driver, expectValidation: false);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving PHQ participant and score updates.");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Target Child Identification", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] PHQ participant and scoring updates saved successfully with toast: {toastMessage}");

            driver.Navigate().GoToUrl($"{_config.AppUrl}/Pages/TCIDs.aspx?pc1id={pc1Id}");
            driver.WaitForReady(15);
            OpenExistingTcidEntry(driver);
            SwitchToTab(driver, PhqTabSelector, phqTabTitle);

            participantDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(PhqParticipantDropdownSelector), 10)
                ?? throw new InvalidOperationException("PHQ participant dropdown was not found when verifying saved values.");
            participantSelect = new SelectElement(participantDropdown);
            Assert.Equal("02", participantSelect.SelectedOption.GetAttribute("value"));

            phqWorkerDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(PhqWorkerDropdownSelector), 10)
                ?? throw new InvalidOperationException("PHQ worker dropdown was not found when verifying saved values.");
            workerSelect = new SelectElement(phqWorkerDropdown);
            Assert.Equal("105", workerSelect.SelectedOption.GetAttribute("value"));

            WaitForLabelText(driver, PhqResultLabelSelector, text => text.Contains("Positive", StringComparison.OrdinalIgnoreCase));
            WaitForLabelText(driver, PhqScoreValidityLabelSelector, text => text.Contains("Valid", StringComparison.OrdinalIgnoreCase));
            _output.WriteLine("[PASS] PHQ participant selection, worker, and scores persisted after saving.");
        }

        private static DateTime ParseDate(string value, string description)
        {
            if (DateTime.TryParseExact(value, AcceptedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Unable to parse {description} '{value}'.");
        }

        private static void SetPhqDate(IPookieWebDriver driver, IWebElement input, DateTime date)
        {
            var formatted = date.ToString(DateFormatShortYear, CultureInfo.InvariantCulture);
            WebElementHelper.SetInputValue(driver, input, formatted, "PHQ date input", triggerBlur: true);
        }

        private static void SetPhqScores(IPookieWebDriver driver, string[] values)
        {
            var dropdowns = driver.FindElements(By.CssSelector(PhqQuestionDropdownsSelector));
            for (var i = 0; i < dropdowns.Count && i < values.Length; i++)
            {
                var dropdown = dropdowns[i];
                if (!dropdown.Enabled)
                {
                    continue;
                }

                var select = new SelectElement(dropdown);
                var value = values[i];
                if (string.IsNullOrWhiteSpace(value))
                {
                    SelectDropdownPlaceholderOption(dropdown, "PHQ question dropdown");
                    continue;
                }

                select.SelectByValue(value);
            }
        }

        private static void ClearPhqScores(IPookieWebDriver driver)
        {
            var dropdowns = driver.FindElements(By.CssSelector(PhqQuestionDropdownsSelector));
            foreach (var dropdown in dropdowns)
            {
                if (!dropdown.Enabled)
                {
                    continue;
                }

                SelectDropdownPlaceholderOption(dropdown, "PHQ question dropdown");
            }
        }

        private void EnsureDifficultyAndReferralCleared(IPookieWebDriver driver)
        {
            var difficultyDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(PhqDifficultyDropdownSelector), 10)
                ?? throw new InvalidOperationException("PHQ difficulty dropdown was not found.");
            SelectDropdownPlaceholderOption(difficultyDropdown, "PHQ difficulty dropdown");

            var referralCheckbox = driver.WaitforElementToBeInDOM(By.CssSelector(PhqDepressionReferralCheckboxSelector), 10)
                ?? throw new InvalidOperationException("PHQ referral checkbox was not found.");
            if (referralCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, referralCheckbox);
                driver.WaitForReady(1);
                Thread.Sleep(200);
            }
        }

        private static string WaitForLabelText(IPookieWebDriver driver, string selector, Func<string, bool> predicate, int timeoutSeconds = 5)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            string lastValue = string.Empty;
            while (DateTime.Now <= endTime)
            {
                var element = driver.FindElements(By.CssSelector(selector)).FirstOrDefault();
                if (element != null)
                {
                    lastValue = element.Text?.Trim() ?? string.Empty;
                    if (predicate(lastValue))
                    {
                        return lastValue;
                    }
                }

                Thread.Sleep(200);
            }

            throw new TimeoutException($"Label '{selector}' did not reach expected state within {timeoutSeconds} seconds. Last value '{lastValue}'.");
        }
    }
}

