using System;
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
        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(9)]
        public void MiechvMedicalCareSourceOtherSpecifyToggle(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenExistingTcidEntry(driver);

            SwitchToTab(driver, MiechvTabSelector, "MIECHV");

            var careSourceDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalCareSourceDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical care source dropdown was not found on MIECHV tab.");
            var careSourceSelect = new SelectElement(careSourceDropdown);

            _output.WriteLine("[INFO] Selecting 'Other' for medical care source to expose specify input.");
            careSourceSelect.SelectByValue("06");

            var specifyInput = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalCareSourceSpecifyInputSelector), 10)
                ?? throw new InvalidOperationException("Medical care source specify input was not found.");
            Assert.True(specifyInput.Displayed, "Medical care source specify input should be visible when 'Other' is selected.");
            WebElementHelper.SetInputValue(driver, specifyInput, "Community clinic", "Medical care source specify input", triggerBlur: true);

            _output.WriteLine("[INFO] Selecting another option to hide specify input.");
            careSourceSelect.SelectByValue("02");
            driver.WaitForReady(1);
            Thread.Sleep(200);
            Assert.False(specifyInput.Displayed, "Medical care source specify input should be hidden when selecting non-'Other' option.");

            SubmitForm(driver, expectValidation: false);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving MIECHV updates.");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Target Child Identification", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] MIECHV medical care source updates saved successfully with toast: {toastMessage}");

            driver.Navigate().GoToUrl($"{_config.AppUrl}/Pages/TCIDs.aspx?pc1id={pc1Id}");
            driver.WaitForReady(15);
            OpenExistingTcidEntry(driver);
            SwitchToTab(driver, MiechvTabSelector, "MIECHV");

            careSourceDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalCareSourceDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical care source dropdown was not found when verifying saved values.");
            careSourceSelect = new SelectElement(careSourceDropdown);
            Assert.Equal("02", careSourceSelect.SelectedOption.GetAttribute("value"));
            _output.WriteLine("[PASS] Medical care source selection persisted after saving.");
        }
    }
}

