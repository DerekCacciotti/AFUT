using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.Pages;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.BaselineForm
{
    /// <summary>
    /// Tests for PC1 Medical Provider/Benefits tab of the Baseline Form
    /// </summary>
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class BaselineFormPC1MedicalProviderTests : IClassFixture<AppConfig>
    {
        protected readonly AppConfig _config;
        protected readonly IPookieDriverFactory _driverFactory;
        protected readonly ITestOutputHelper _output;
        protected static readonly Random RandomGenerator = new();
        protected static readonly object RandomLock = new();

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public BaselineFormPC1MedicalProviderTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(3)]
        public void MedicalProviderTabCompleteFlowTest(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToBaselineForm(driver, formsPane);
            
            // Navigate to Medical Provider/Benefits tab
            ActivateTab(driver, "#tab_MEDICAL a[href='#MEDICAL']", "PC1 Medical Provider/Benefits");
            _output.WriteLine("[PASS] Medical Provider/Benefits tab activated successfully");

            // ===== PART 1: OBP Involvement Validation =====
            _output.WriteLine("\n[TEST SECTION] Testing OBP Involvement validation");

            // Select "Other" in OBP Involvement dropdown
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlOBPInvolvement']",
                "Involvement of OBP dropdown",
                "7. Other",
                "07");
            _output.WriteLine("[INFO] Selected 'Other' in OBP Involvement dropdown");

            // Verify specify textbox appears
            var specifyDiv = driver.WaitforElementToBeInDOM(By.CssSelector("#divOBPInvolvementSpecify"), 5)
                ?? throw new InvalidOperationException("OBP Involvement Specify div was not found.");
            Assert.True(specifyDiv.Displayed, "OBP Involvement Specify div should be visible when 'Other' is selected.");
            _output.WriteLine("[INFO] OBP Involvement Specify field is visible");

            // Clear the specify textbox if it has any value
            var specifyInput = specifyDiv.FindElements(By.CssSelector("input.form-control[id*='txtOBPInvolvementSpecify']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("OBP Involvement Specify input was not found.");
            specifyInput.Clear();
            _output.WriteLine("[INFO] Cleared OBP Involvement Specify field");

            // Click submit without filling specify field
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit button without filling specify field");

            // Verify validation message
            var obpValidation = FindValidationMessage(driver, "OBP Involvement specify validation", "Please specify involvement of OBP");
            Assert.NotNull(obpValidation);
            _output.WriteLine($"[PASS] OBP validation displayed: {obpValidation!.Text.Trim()}");

            // Verify tab resets to PC1
            var pc1Tab = driver.FindElements(By.CssSelector("#tab_PC1")).FirstOrDefault();
            if (pc1Tab != null)
            {
                var isActive = pc1Tab.GetAttribute("class")?.Contains("active", StringComparison.OrdinalIgnoreCase) ?? false;
                if (isActive)
                {
                    _output.WriteLine("[INFO] Tab reset to PC1 after validation (expected behavior)");
                }
            }

            // Switch back to Medical tab
            ActivateTab(driver, "#tab_MEDICAL a[href='#MEDICAL']", "PC1 Medical Provider/Benefits");
            _output.WriteLine("[INFO] Switched back to Medical Provider/Benefits tab");

            // Select a random option (not "Other")
            var obpDropdown = WebElementHelper.FindElementInModalOrPage(
                driver,
                "select.form-control[id*='ddlOBPInvolvement']",
                "OBP Involvement dropdown",
                10);
            var selectElement = new SelectElement(obpDropdown);
            var validOptions = selectElement.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && opt.GetAttribute("value") != "07")
                .ToList();

            if (validOptions.Any())
            {
                var randomIndex = GetRandomNumber(0, validOptions.Count - 1);
                var randomOption = validOptions[randomIndex];
                var optionText = randomOption.Text.Trim();
                var optionValue = randomOption.GetAttribute("value");

                selectElement.SelectByValue(optionValue);
                driver.WaitForUpdatePanel(5);
                driver.WaitForReady(5);
                Thread.Sleep(250);
                _output.WriteLine($"[INFO] Selected random OBP option: {optionText} (value: {optionValue})");
            }
            else
            {
                _output.WriteLine("[WARN] No non-Other options available in OBP dropdown");
            }

            _output.WriteLine("[PASS] OBP Involvement validation test completed successfully");

            // ===== PART 2: Add New Medical Provider =====
            _output.WriteLine("\n[TEST SECTION] Testing Add New Medical Provider");

            // Ensure "Does Primary Caregiver 1 have a Medical Provider?" is set to Yes
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPC1HasMedicalProvider']",
                "PC1 Has Medical Provider dropdown",
                "Yes",
                "1");
            driver.WaitForReady(3);
            _output.WriteLine("[INFO] Set PC1 Has Medical Provider to Yes");

            // Click "Not in List" link
            var notInListLink = driver.FindElements(By.CssSelector("a.btn.btn-link[id*='lnkNewMedicalProvider']"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Not in List", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Not in List link for medical provider was not found.");
            
            CommonTestHelper.ClickElement(driver, notInListLink);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked 'Not in List' link");

            // Click Submit in the modal without filling fields
            var modalSubmitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary.custom-submit-button[data-validation-group='MedicalProvider']",
                "Medical Provider modal Submit button",
                15);
            
            CommonTestHelper.ClickElement(driver, modalSubmitButton);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit button in modal without filling fields");

            // Verify validation message "Provider's Last Name required"
            var lastNameValidation = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Provider's Last Name required", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(lastNameValidation);
            _output.WriteLine($"[PASS] Last Name validation displayed: {lastNameValidation!.Text.Trim()}");

            // Generate unique timestamp for provider name
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // Fill all provider details
            var firstNameInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-firstName.form-control",
                "Provider First Name input",
                10);
            WebElementHelper.SetInputValue(driver, firstNameInput, $"PC1medicalproviderFirstNameTest{timestamp}", "Provider First Name", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered Provider First Name: PC1medicalproviderFirstNameTest{timestamp}");

            var lastNameInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-lastName.form-control",
                "Provider Last Name input",
                10);
            WebElementHelper.SetInputValue(driver, lastNameInput, $"PC1medicalProviderLastNameTest{timestamp}", "Provider Last Name", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered Provider Last Name: PC1medicalProviderLastNameTest{timestamp}");

            var addressInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-address.form-control",
                "Provider Address input",
                10);
            WebElementHelper.SetInputValue(driver, addressInput, "PC1medicalProvideraddressTest", "Provider Address", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Provider Address");

            var cityInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-city.form-control",
                "Provider City input",
                10);
            WebElementHelper.SetInputValue(driver, cityInput, "PC1medicalProviderCity", "Provider City", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Provider City");

            // Enter State (text input, not dropdown)
            var stateInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-state.form-control",
                "Provider State input",
                10);
            WebElementHelper.SetInputValue(driver, stateInput, "AA", "Provider State", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Provider State: AA");

            var zipCodeInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-zip.form-control",
                "Provider Zip Code input",
                10);
            WebElementHelper.SetInputValue(driver, zipCodeInput, "00000", "Provider Zip Code", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Provider Zip Code");

            var phoneInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-phone.form-control",
                "Provider Phone input",
                10);
            WebElementHelper.SetInputValue(driver, phoneInput, "5555555555", "Provider Phone", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Provider Phone");

            // Click Submit to save the provider
            modalSubmitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary.custom-submit-button[data-validation-group='MedicalProvider']",
                "Medical Provider modal Submit button",
                15);
            
            CommonTestHelper.ClickElement(driver, modalSubmitButton);
            driver.WaitForUpdatePanel(15);
            driver.WaitForReady(15);
            Thread.Sleep(2000);
            _output.WriteLine("[INFO] Clicked Submit button to save provider");

            // Wait for modal to close completely
            _output.WriteLine("[INFO] Waiting for modal to close and page to refresh...");
            Thread.Sleep(3000);

            // Wait for modal to disappear
            var modalGone = WaitForModalToClose(driver, 10);
            if (modalGone)
            {
                _output.WriteLine("[INFO] Modal closed successfully");
            }
            else
            {
                _output.WriteLine("[WARN] Modal might still be visible");
            }

            // Ensure we're back on the Medical tab
            ActivateTab(driver, "#tab_MEDICAL a[href='#MEDICAL']", "PC1 Medical Provider/Benefits");
            Thread.Sleep(1000);

            // Verify the provider appears in the dropdown - look for it on the main page, not in modal
            _output.WriteLine("[INFO] Looking for Medical Provider dropdown on the main page...");
            var providerDropdown = driver.FindElements(By.CssSelector("select.form-control"))
                .FirstOrDefault(el => 
                {
                    var id = el.GetAttribute("id") ?? string.Empty;
                    var name = el.GetAttribute("name") ?? string.Empty;
                    return el.Displayed && 
                           (id.Contains("ddlMedicalProvider", StringComparison.OrdinalIgnoreCase) || 
                            name.Contains("MedicalProvider", StringComparison.OrdinalIgnoreCase)) &&
                           !id.Contains("HasMedicalProvider", StringComparison.OrdinalIgnoreCase);
                });

            if (providerDropdown == null)
            {
                _output.WriteLine("[WARN] Medical Provider dropdown not found immediately, waiting...");
                Thread.Sleep(2000);
                providerDropdown = driver.FindElements(By.CssSelector("select.form-control"))
                    .FirstOrDefault(el => 
                    {
                        var id = el.GetAttribute("id") ?? string.Empty;
                        var name = el.GetAttribute("name") ?? string.Empty;
                        return el.Displayed && 
                               (id.Contains("ddlMedicalProvider", StringComparison.OrdinalIgnoreCase) || 
                                name.Contains("MedicalProvider", StringComparison.OrdinalIgnoreCase)) &&
                               !id.Contains("HasMedicalProvider", StringComparison.OrdinalIgnoreCase);
                    });
            }

            Assert.NotNull(providerDropdown);
            _output.WriteLine($"[INFO] Found Medical Provider dropdown: {providerDropdown!.GetAttribute("id")}");
            
            var providerSelect = new SelectElement(providerDropdown);
            _output.WriteLine($"[INFO] Medical Provider dropdown has {providerSelect.Options.Count} options");

            var newProviderOption = providerSelect.Options
                .FirstOrDefault(opt => 
                    opt.Text.Contains($"PC1medicalproviderFirstNameTest{timestamp}", StringComparison.OrdinalIgnoreCase) ||
                    opt.Text.Contains($"PC1medicalProviderLastNameTest{timestamp}", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(newProviderOption);
            _output.WriteLine($"[INFO] New provider found in dropdown: {newProviderOption!.Text.Trim()}");

            // Select the new provider
            providerSelect.SelectByValue(newProviderOption.GetAttribute("value"));
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);
            _output.WriteLine($"[PASS] Successfully selected new provider: {newProviderOption.Text.Trim()}");

            // ===== PART 3: Add New Medical Facility =====
            _output.WriteLine("\n[TEST SECTION] Testing Add New Medical Facility");

            // Click "Not in List" link for facility
            var notInListFacilityLink = driver.FindElements(By.CssSelector("a.btn.btn-link.custom-btn-link#lnkNewMedicalFacility"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Not in List", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Not in List link for medical facility was not found.");
            
            CommonTestHelper.ClickElement(driver, notInListFacilityLink);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked 'Not in List' link for Medical Facility");

            // Click Submit in the modal without filling fields
            var facilitySubmitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary.custom-submit-button[data-validation-group='MedicalFacility']",
                "Medical Facility modal Submit button",
                15);
            
            CommonTestHelper.ClickElement(driver, facilitySubmitButton);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit button in facility modal without filling fields");

            // Verify validation message "Facility Name required"
            var facilityNameValidation = driver.FindElements(By.CssSelector(".validation-summary.alert.alert-danger"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Facility Name required", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(facilityNameValidation);
            _output.WriteLine($"[PASS] Facility Name validation displayed: {facilityNameValidation!.Text.Trim()}");

            // Fill facility details
            var facilityNameInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-name.form-control",
                "Facility Name input",
                10);
            WebElementHelper.SetInputValue(driver, facilityNameInput, "PC1FacilityNameTest", "Facility Name", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Facility Name: PC1FacilityNameTest");

            var facilityAddressInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-address.form-control",
                "Facility Address input",
                10);
            WebElementHelper.SetInputValue(driver, facilityAddressInput, "PC1medicalProvideraddressTest", "Facility Address", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Facility Address");

            var facilityCityInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-city.form-control",
                "Facility City input",
                10);
            WebElementHelper.SetInputValue(driver, facilityCityInput, "PC1medicalProviderCity", "Facility City", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Facility City");

            var facilityStateInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-state.form-control",
                "Facility State input",
                10);
            WebElementHelper.SetInputValue(driver, facilityStateInput, "AA", "Facility State", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Facility State: AA");

            var facilityZipCodeInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-zip.form-control",
                "Facility Zip Code input",
                10);
            WebElementHelper.SetInputValue(driver, facilityZipCodeInput, "00000", "Facility Zip Code", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Facility Zip Code");

            var facilityPhoneInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "input.txt-phone.form-control",
                "Facility Phone input",
                10);
            WebElementHelper.SetInputValue(driver, facilityPhoneInput, "5555555555", "Facility Phone", triggerBlur: true);
            _output.WriteLine("[INFO] Entered Facility Phone");

            // Click Submit to save the facility
            facilitySubmitButton = WebElementHelper.FindElementInModalOrPage(
                driver,
                "a.btn.btn-primary.custom-submit-button[data-validation-group='MedicalFacility']",
                "Medical Facility modal Submit button",
                15);
            
            CommonTestHelper.ClickElement(driver, facilitySubmitButton);
            driver.WaitForUpdatePanel(15);
            driver.WaitForReady(15);
            Thread.Sleep(2000);
            _output.WriteLine("[INFO] Clicked Submit button to save facility");

            // Wait for modal to close
            _output.WriteLine("[INFO] Waiting for facility modal to close...");
            Thread.Sleep(3000);
            WaitForModalToClose(driver, 10);

            // Ensure we're back on the Medical tab
            ActivateTab(driver, "#tab_MEDICAL a[href='#MEDICAL']", "PC1 Medical Provider/Benefits");
            Thread.Sleep(1000);

            // Select the new facility from dropdown
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPC1MedicalFacility']",
                "PC1 Medical Facility dropdown",
                "PC1FacilityNameTest",
                null);
            _output.WriteLine("[PASS] Successfully selected new facility: PC1FacilityNameTest");

            // ===== PART 4: Test Medicaid and Health Insurance =====
            _output.WriteLine("\n[TEST SECTION] Testing Medicaid and Health Insurance interactions");

            // Test Medicaid dropdown - select No (should NOT show textbox)
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPC1ReceivingMedicaid']",
                "PC1 Receiving Medicaid dropdown",
                "No",
                "0");
            Thread.Sleep(500);

            var medicaidTextboxHidden = driver.FindElements(By.CssSelector("#divHIMedicaidCaseNumber"))
                .All(el => !el.Displayed);
            Assert.True(medicaidTextboxHidden, "Medicaid Case Number textbox should be hidden when selecting No");
            _output.WriteLine("[PASS] Medicaid textbox hidden when selecting No");

            // Verify Health Insurance checkboxes are enabled
            var healthInsuranceCheckboxes = driver.FindElements(By.CssSelector("#divHealthInsurance input[type='checkbox']")).ToList();
            Assert.True(healthInsuranceCheckboxes.All(cb => cb.Enabled), "Health Insurance checkboxes should be enabled when Medicaid is No");
            _output.WriteLine("[PASS] Health Insurance checkboxes are enabled when Medicaid is No");

            // Test selecting Unknown
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPC1ReceivingMedicaid']",
                "PC1 Receiving Medicaid dropdown",
                "Unknown",
                "U");
            Thread.Sleep(500);

            medicaidTextboxHidden = driver.FindElements(By.CssSelector("#divHIMedicaidCaseNumber"))
                .All(el => !el.Displayed);
            Assert.True(medicaidTextboxHidden, "Medicaid Case Number textbox should be hidden when selecting Unknown");
            _output.WriteLine("[PASS] Medicaid textbox hidden when selecting Unknown");

            // Select Yes for Medicaid
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPC1ReceivingMedicaid']",
                "PC1 Receiving Medicaid dropdown",
                "Yes",
                "1");
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            // Verify Medicaid Case Number textbox appears
            var medicaidTextbox = driver.FindElements(By.CssSelector("#divHIMedicaidCaseNumber"))
                .FirstOrDefault(el => el.Displayed);
            Assert.NotNull(medicaidTextbox);
            _output.WriteLine("[PASS] Medicaid Case Number textbox displayed when selecting Yes");

            // Verify Health Insurance checkboxes are disabled
            healthInsuranceCheckboxes = driver.FindElements(By.CssSelector("#divHealthInsurance input[type='checkbox']")).ToList();
            Assert.True(healthInsuranceCheckboxes.All(cb => !cb.Enabled), "Health Insurance checkboxes should be disabled when Medicaid is Yes");
            _output.WriteLine("[PASS] Health Insurance checkboxes are disabled when Medicaid is Yes");

            // Change back to No to enable health insurance
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPC1ReceivingMedicaid']",
                "PC1 Receiving Medicaid dropdown",
                "No",
                "0");
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            // Click "Other" checkbox
            var otherCheckbox = driver.FindElements(By.CssSelector("input[type='checkbox'][id*='chkHIOther']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Health Insurance 'Other' checkbox was not found.");
            
            if (!otherCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, otherCheckbox);
                driver.WaitForReady(3);
                Thread.Sleep(500);
            }
            _output.WriteLine("[INFO] Clicked 'Other' Health Insurance checkbox");

            // Verify specify textbox appears
            var otherSpecifyDiv = driver.FindElements(By.CssSelector("#divHIOtherSpecify"))
                .FirstOrDefault(el => el.Displayed);
            Assert.NotNull(otherSpecifyDiv);
            _output.WriteLine("[PASS] Health Insurance 'Other' specify textbox appeared");

            // ===== PART 5: Current Service Involvement =====
            _output.WriteLine("\n[TEST SECTION] Testing Current Service Involvement dropdowns");

            // Select random values for all 4 service involvement dropdowns
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlSIMentalHealth']", "Mental Health dropdown");
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlSISubstanceAbuse']", "Substance Abuse dropdown");
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlSIDomesticViolence']", "Domestic Violence dropdown");
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlICPSACS']", "CPS/ACS dropdown");
            _output.WriteLine("[PASS] Selected random values in all Current Service Involvement dropdowns");

            // ===== PART 6: Public Benefits =====
            _output.WriteLine("\n[TEST SECTION] Testing Public Benefits validations");

            // Select Yes for receiving public benefits
            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlReceivingPublicBenefits']",
                "Receiving Public Benefits dropdown",
                "Yes",
                "1");
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(500);
            _output.WriteLine("[INFO] Selected Yes for receiving public benefits");

            // Click Submit to trigger validations
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit to trigger public benefits validations");

            // Switch back to Medical tab (page may reset to PC1)
            ActivateTab(driver, "#tab_MEDICAL a[href='#MEDICAL']", "PC1 Medical Provider/Benefits");
            Thread.Sleep(500);

            // Verify all 5 benefit validation messages
            var tanfValidation = FindValidationMessage(driver, "TANF validation", "TANF", "required", "PC1 Medical Provider/Benefits");
            Assert.NotNull(tanfValidation);
            _output.WriteLine($"[PASS] TANF validation displayed: {tanfValidation!.Text.Trim()}");

            var foodStampsValidation = FindValidationMessage(driver, "Food Stamps validation", "Food Stamps", "required", "PC1 Medical Provider/Benefits");
            Assert.NotNull(foodStampsValidation);
            _output.WriteLine($"[PASS] Food Stamps validation displayed: {foodStampsValidation!.Text.Trim()}");

            var emergencyAssistanceValidation = FindValidationMessage(driver, "Emergency Assistance validation", "Emergency Assistance", "required", "PC1 Medical Provider/Benefits");
            Assert.NotNull(emergencyAssistanceValidation);
            _output.WriteLine($"[PASS] Emergency Assistance validation displayed: {emergencyAssistanceValidation!.Text.Trim()}");

            var wicValidation = FindValidationMessage(driver, "WIC validation", "WIC", "required", "PC1 Medical Provider/Benefits");
            Assert.NotNull(wicValidation);
            _output.WriteLine($"[PASS] WIC validation displayed: {wicValidation!.Text.Trim()}");

            var ssiValidation = FindValidationMessage(driver, "SSI/SSD validation", "SSI/SSD", "required", "PC1 Medical Provider/Benefits");
            Assert.NotNull(ssiValidation);
            _output.WriteLine($"[PASS] SSI/SSD validation displayed: {ssiValidation!.Text.Trim()}");

            // Fill TANF and verify individual validation clears
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlPBTANF']", "TANF dropdown");
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            ActivateTab(driver, "#tab_MEDICAL a[href='#MEDICAL']", "PC1 Medical Provider/Benefits");
            Thread.Sleep(500);

            var tanfValidationGone = FindValidationMessage(driver, "TANF validation check", "TANF", "required");
            _output.WriteLine(tanfValidationGone == null ? "[PASS] TANF validation cleared after selection" : "[INFO] TANF validation still present (other validations showing)");

            // Fill all remaining benefit dropdowns
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlPBFS']", "Food Stamps dropdown");
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlPBEmergencyAssistance']", "Emergency Assistance dropdown");
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlPBWIC']", "WIC dropdown");
            SelectRandomDropdownOption(driver, "select.form-control[id*='ddlPBSSI']", "SSI/SSD dropdown");
            _output.WriteLine("[PASS] Selected random values in all public benefit dropdowns");

            // Final Submit
            _output.WriteLine("\n[TEST SECTION] Final form submission");
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);
            _output.WriteLine("[INFO] Clicked final Submit button");

            // Verify success toast message
            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving the form.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Form saved successfully with toast message: {toastMessage}");

            _output.WriteLine("\n[PASS] Medical Provider/Benefits tab complete flow test finished successfully");
        }

        protected void NavigateToBaselineForm(IPookieWebDriver driver, IWebElement formsPane)
        {
            var baselineFormLink = formsPane.FindElements(By.CssSelector(
                    "a.list-group-item.moreInfo[href*='Intake.aspx'], " +
                    "a.moreInfo[data-formtype='in'], " +
                    "a.list-group-item[title='Intake']"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Baseline", StringComparison.OrdinalIgnoreCase) ?? false))
                ?? throw new InvalidOperationException("Baseline Form link was not found in the Forms tab.");

            _output.WriteLine($"[INFO] Found Baseline Form link: {baselineFormLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, baselineFormLink);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url ?? string.Empty;
            Assert.Contains("Intake.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ipk=", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Navigated to Intake page: {currentUrl}");
        }

        protected void ActivateTab(IPookieWebDriver driver, string tabSelector, string tabName)
        {
            var tabLink = driver.WaitforElementToBeInDOM(By.CssSelector(tabSelector), 10)
                ?? throw new InvalidOperationException($"Tab link '{tabName}' was not found.");
            CommonTestHelper.ClickElement(driver, tabLink);
            driver.WaitForReady(5);
            Thread.Sleep(500);
            _output.WriteLine($"[INFO] Activated {tabName} tab");
        }

        protected IWebElement FindSubmitButton(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Submit", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    (el.GetAttribute("title")?.Contains("Save", StringComparison.OrdinalIgnoreCase) ?? true))
                ?? throw new InvalidOperationException("Submit button was not found.");
        }

        protected IWebElement? FindValidationMessage(
            IPookieWebDriver driver,
            string description,
            params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                throw new ArgumentException("At least one keyword is required to locate a validation message.", nameof(keywords));
            }

            var candidates = driver.FindElements(By.CssSelector(
                    ".text-danger, span.text-danger, span[style*='color: red'], span[style*='color:Red'], " +
                    "div.alert.alert-danger, .validation-summary-errors li, .validation-summary"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .ToList();

            foreach (var element in candidates)
            {
                var text = element.Text.Trim();
                if (keywords.All(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    return element;
                }
            }

            if (!candidates.Any())
            {
                _output.WriteLine($"[WARN] {description}: no visible validation messages found after submit.");
            }
            else
            {
                _output.WriteLine($"[WARN] {description}: visible validation messages did not match expected keywords ({string.Join(", ", keywords)}).");
                foreach (var element in candidates)
                {
                    _output.WriteLine($"[WARN] Validation message found: \"{element.Text.Trim()}\"");
                }
            }

            return null;
        }

        protected static int GetRandomNumber(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            lock (RandomLock)
            {
                return RandomGenerator.Next(minInclusive, maxInclusive + 1);
            }
        }

        protected bool WaitForModalToClose(IPookieWebDriver driver, int timeoutSeconds)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= endTime)
            {
                var modals = driver.FindElements(By.CssSelector(".modal.show, .modal.in, .modal[style*='display: block'], .modal.fade.in"));
                var visibleModal = modals.FirstOrDefault(m => m.Displayed);
                
                if (visibleModal == null)
                {
                    return true;
                }
                
                Thread.Sleep(500);
            }
            return false;
        }

        protected void SelectRandomDropdownOption(IPookieWebDriver driver, string cssSelector, string description)
        {
            var dropdown = WebElementHelper.FindElementInModalOrPage(driver, cssSelector, description, 10);
            var selectElement = new SelectElement(dropdown);
            var validOptions = selectElement.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")))
                .ToList();

            if (!validOptions.Any())
            {
                throw new InvalidOperationException($"No selectable options were found for {description}.");
            }

            var randomIndex = GetRandomNumber(0, validOptions.Count - 1);
            var randomOption = validOptions[randomIndex];
            var optionText = randomOption.Text.Trim();
            var optionValue = randomOption.GetAttribute("value");

            selectElement.SelectByValue(optionValue);
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);

            _output.WriteLine($"[INFO] Selected random option in {description}: {optionText} (value: {optionValue})");
        }
    }
}

