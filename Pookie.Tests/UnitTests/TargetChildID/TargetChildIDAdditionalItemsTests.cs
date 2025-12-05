using System;
using System.Linq;
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
        [TestPriority(7)]
        public void AdditionalItemsParityAndDeliveryFlow(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenExistingTcidEntry(driver);

            const string additionalItemsTab = "#OptionalItems";

            SwitchToTab(driver, additionalItemsTab, "Additional Items");

            var parityDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(ParityDropdownSelector), 10)
                ?? throw new InvalidOperationException("Parity dropdown was not found on the Additional Items tab.");

            _output.WriteLine("[INFO] Clearing parity selection to trigger validation.");
            SelectDropdownPlaceholderOption(parityDropdown, "Parity dropdown");

            var summaryText = SubmitFormFromAdditionalItemsTab(driver);
            Assert.Contains("Parity is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Parity validation appeared after submitting with placeholder selected.");

            SwitchToTab(driver, additionalItemsTab, "Additional Items");
            SelectRandomDropdownOption(driver, ParityDropdownSelector, "Parity dropdown");
            parityDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(ParityDropdownSelector), 10)
                ?? throw new InvalidOperationException("Parity dropdown was not found after selecting a value.");
            var paritySelect = new SelectElement(parityDropdown);
            var expectedParityValue = paritySelect.SelectedOption.GetAttribute("value") ?? string.Empty;
            _output.WriteLine($"[INFO] Selected parity value {expectedParityValue}.");

            SelectRandomDropdownOption(driver, DeliveryTypeDropdownSelector, "Delivery type dropdown");
            var deliveryDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(DeliveryTypeDropdownSelector), 10)
                ?? throw new InvalidOperationException("Delivery type dropdown was not found after selecting a value.");
            var deliverySelect = new SelectElement(deliveryDropdown);
            var expectedDeliveryValue = deliverySelect.SelectedOption.GetAttribute("value") ?? string.Empty;
            _output.WriteLine($"[INFO] Selected delivery type value {expectedDeliveryValue}.");

            SelectRandomDropdownOption(driver, ChildFedBreastMilkDropdownSelector, "Child fed breast milk dropdown");
            var breastDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(ChildFedBreastMilkDropdownSelector), 10)
                ?? throw new InvalidOperationException("Child fed breast milk dropdown was not found after selecting a value.");
            var breastSelect = new SelectElement(breastDropdown);
            var expectedBreastValue = breastSelect.SelectedOption.GetAttribute("value") ?? string.Empty;
            _output.WriteLine($"[INFO] Selected breast milk value {expectedBreastValue}.");

            var tooltipIcons = driver.FindElements(By.CssSelector(AdditionalItemsTooltipSelector))
                .Where(icon => icon.Displayed)
                .ToList();
            Assert.True(tooltipIcons.Count >= 2, "Expected at least two tooltip icons on Additional Items tab.");

            foreach (var icon in tooltipIcons.Take(2))
            {
                CommonTestHelper.ClickElement(driver, icon);
                driver.WaitForReady(1);
                Thread.Sleep(300);
                var tooltipElement = driver.FindElements(By.CssSelector(".tooltip-inner"))
                    .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

                Assert.NotNull(tooltipElement);
                _output.WriteLine($"[INFO] Tooltip text: {tooltipElement?.Text}");
            }
            _output.WriteLine("[PASS] Tooltip question mark icons displayed helper text when interacted with.");

            _output.WriteLine("[INFO] Submitting TCID form after updating Additional Items.");
            SubmitFormFromAdditionalItemsTab(driver, expectValidation: false);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving Additional Items.");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Target Child Identification", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] TCID form saved successfully with toast: {toastMessage}");

            _output.WriteLine("[INFO] Returning to TCID grid to reopen the same entry.");
            driver.Navigate().GoToUrl($"{_config.AppUrl}/Pages/TCIDs.aspx?pc1id={pc1Id}");
            driver.WaitForReady(15);
            OpenExistingTcidEntry(driver);
            SwitchToTab(driver, additionalItemsTab, "Additional Items");

            parityDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(ParityDropdownSelector), 10)
                ?? throw new InvalidOperationException("Parity dropdown was not found when verifying saved values.");
            paritySelect = new SelectElement(parityDropdown);
            Assert.Equal(expectedParityValue, paritySelect.SelectedOption.GetAttribute("value"));

            deliveryDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(DeliveryTypeDropdownSelector), 10)
                ?? throw new InvalidOperationException("Delivery type dropdown was not found when verifying saved values.");
            deliverySelect = new SelectElement(deliveryDropdown);
            Assert.Equal(expectedDeliveryValue, deliverySelect.SelectedOption.GetAttribute("value"));

            breastDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(ChildFedBreastMilkDropdownSelector), 10)
                ?? throw new InvalidOperationException("Child breast milk dropdown was not found when verifying saved values.");
            breastSelect = new SelectElement(breastDropdown);
            Assert.Equal(expectedBreastValue, breastSelect.SelectedOption.GetAttribute("value"));
            _output.WriteLine("[PASS] Additional Items selections persisted after saving.");
        }
    }
}

