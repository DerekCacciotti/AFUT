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

namespace AFUT.Tests.UnitTests.IdContactInformation
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class IdContactInformationTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;
        private string TargetPc1Id => _config.TestPc1Id;

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public IdContactInformationTests(AppConfig config, ITestOutputHelper output)
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
        public void CheckingNavigationToIdContactInformationPage(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Identification and Contact Information
            NavigateToIdContactInformation(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Identification and Contact Information page");

            // Verify PC1 ID is present on the page
            var pc1Display = CommonTestHelper.FindPc1Display(driver, pc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on Identification and Contact Information page.");
            Assert.Contains(pc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Verified PC1 ID display: {pc1Display}");

            // Verify page elements are present
            var formContainer = driver.WaitforElementToBeInDOM(By.CssSelector(
                ".panel-body, " +
                ".form-horizontal, " +
                "form, " +
                ".container-fluid"), 10);

            Assert.NotNull(formContainer);
            _output.WriteLine("[PASS] Identification and Contact Information form container is present on the page");
            
            // Log form elements for debugging
            var formElements = driver.FindElements(By.CssSelector(
                "input.form-control, " +
                "select.form-control, " +
                "textarea.form-control, " +
                "input[type='radio'], " +
                "input[type='checkbox']"));
            
            _output.WriteLine($"[INFO] Found {formElements.Count} form elements on the page");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void CheckingAllTabsOnIdContactInformationPage(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Identification and Contact Information
            NavigateToIdContactInformation(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Identification and Contact Information page");

            // Click "Edit Primary Caregiver 1" button, verify edit page, and submit
            ClickEditPrimaryCaregiver1Button(driver);
            _output.WriteLine("[PASS] Clicked Edit Primary Caregiver 1 button and submitted form");

            // Test Tab 1: Primary Caregiver 1 (PC1) - Should be active by default
            _output.WriteLine("\n[INFO] Testing Tab 1: Primary Caregiver 1");
            VerifyPrimaryCaregiver1Tab(driver, pc1Id);
            _output.WriteLine("[PASS] Primary Caregiver 1 tab verified successfully");

            // Test Tab 2: Other Biological Parent (OBP)
            _output.WriteLine("\n[INFO] Testing Tab 2: Other Biological Parent");
            ClickAndVerifyTab(driver, "OBP", "Other Biological Parent");
            VerifyOtherBiologicalParentTab(driver);
            _output.WriteLine("[PASS] Other Biological Parent tab verified successfully");

            // Test Tab 3: Primary Caregiver 2 (PC2)
            _output.WriteLine("\n[INFO] Testing Tab 3: Primary Caregiver 2");
            ClickAndVerifyTab(driver, "PC2", "Primary Caregiver 2");
            VerifyPrimaryCaregiver2Tab(driver);
            _output.WriteLine("[PASS] Primary Caregiver 2 tab verified successfully");

            // Test Tab 4: Emergency Contact Person
            _output.WriteLine("\n[INFO] Testing Tab 4: Emergency Contact Person");
            ClickAndVerifyTab(driver, "Emergency", "Emergency Contact Person");
            VerifyEmergencyContactTab(driver);
            _output.WriteLine("[PASS] Emergency Contact Person tab verified successfully");

            // Test Tab 5: Informed Consent (Agreement)
            _output.WriteLine("\n[INFO] Testing Tab 5: Informed Consent");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            VerifyInformedConsentTab(driver);
            _output.WriteLine("[PASS] Informed Consent tab verified successfully");

            _output.WriteLine("\n[PASS] All 5 tabs successfully clicked and verified!");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(3)]
        public void CheckingValidationOnInformedConsentTab(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Identification and Contact Information
            NavigateToIdContactInformation(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Identification and Contact Information page");

            // Click "Edit Primary Caregiver 1" button, verify edit page, and submit
            ClickEditPrimaryCaregiver1Button(driver);
            _output.WriteLine("[PASS] Clicked Edit Primary Caregiver 1 button and submitted form");

            // Navigate to the last tab (Informed Consent)
            _output.WriteLine("\n[INFO] Navigating to Informed Consent tab");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            _output.WriteLine("[PASS] Informed Consent tab is now active");

            _output.WriteLine("[INFO] Clearing Signed Confidentiality Agreement selection to trigger validation");
            ClearConfidentialityAgreementSelection(driver);
            _output.WriteLine("[PASS] Signed Confidentiality Agreement selection cleared");

            // Click the Submit button on the Informed Consent tab
            _output.WriteLine("\n[INFO] Attempting to submit without signing confidentiality agreement");
            ClickSubmitButtonOnInformedConsentTab(driver);
            _output.WriteLine("[INFO] Clicked Submit button");

            // Wait for validation to complete
            Thread.Sleep(2000);

            // First, let's log all validation errors we can find
            var allValidationErrors = driver.FindElements(By.CssSelector(
                "span[style*='color:Red'], " +
                "span[style*='color: red'], " +
                ".text-danger, " +
                "span.text-danger, " +
                "div.alert.alert-danger, " +
                "div.validation-summary-errors"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .ToList();

            _output.WriteLine($"[DEBUG] Found {allValidationErrors.Count} validation error elements");
            foreach (var error in allValidationErrors)
            {
                _output.WriteLine($"[DEBUG] Validation error text: {error.Text.Trim()}");
            }

            // Look for the confidentiality agreement validation error
            var validationError = allValidationErrors
                .FirstOrDefault(el => el.Text.Contains("confidentiality agreement", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(validationError);
            var validationText = validationError.Text.Trim();
            _output.WriteLine($"[INFO] Validation message: {validationText}");

            Assert.Contains("22", validationText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("confidentiality agreement", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Confidentiality agreement validation message displayed correctly");
        }

        /// <summary>
        /// Tests the complete workflow of filling out the Identification and Contact Information form
        /// including OBP and PC2 assignment, removal, and final submission.
        /// 
        /// IMPORTANT: This test will fail if the form has already been submitted for the test PC1 ID,
        /// as it expects to start with a blank/new form. If the form data already exists from a previous run,
        /// the test should be run after cleaning the test data or use Test 5 to edit existing data.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(4)]
        public void CheckingSuccessfulSubmissionAfterAnsweringAllRequiredQuestions(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Important warning message for test execution
            _output.WriteLine("================================================================================");
            _output.WriteLine("[WARN] This test expects a BLANK/NEW form for PC1 ID: " + pc1Id);
            _output.WriteLine("[WARN] If form data already exists from a previous run, this test will FAIL.");
            _output.WriteLine("[WARN] Please ensure test data is clean OR run Test 5 to edit existing data.");
            _output.WriteLine("================================================================================");
            _output.WriteLine("");

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Identification and Contact Information
            NavigateToIdContactInformation(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Identification and Contact Information page");

            // Click "Edit Primary Caregiver 1" button, verify edit page, and submit
            ClickEditPrimaryCaregiver1Button(driver);
            _output.WriteLine("[PASS] Clicked Edit Primary Caregiver 1 button and submitted form");

            // Navigate to the last tab (Informed Consent)
            _output.WriteLine("\n[INFO] Navigating to Informed Consent tab");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            _output.WriteLine("[PASS] Informed Consent tab is now active");

            // Answer question 22: Signed confidentiality agreement with "Yes"
            _output.WriteLine("\n[INFO] Selecting 'Yes' for Signed Confidentiality Agreement (Question 22)");
            SelectConfidentialityAgreement(driver, "Yes");
            _output.WriteLine("[PASS] Selected 'Yes' for confidentiality agreement");

            // Click the Submit button - should show 2 validation errors (OBP and PC2)
            _output.WriteLine("\n[INFO] Submitting form - expecting validation errors for OBP and PC2 tabs");
            ClickSubmitButtonOnInformedConsentTab(driver);
            _output.WriteLine("[INFO] Clicked Submit button");

            // Wait for validation to complete
            Thread.Sleep(2000);

            // Verify TWO validation errors appear
            var validationErrors = GetAllValidationErrors(driver);
            _output.WriteLine($"[INFO] Found {validationErrors.Count} validation error(s)");
            
            var obpError = validationErrors.FirstOrDefault(e => 
                e.Contains("OBP", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("Other Biological Parent", StringComparison.OrdinalIgnoreCase));
            var pc2Error = validationErrors.FirstOrDefault(e => 
                e.Contains("PC2", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("Primary Caregiver 2", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(obpError);
            Assert.NotNull(pc2Error);
            _output.WriteLine($"[PASS] OBP validation error: {obpError}");
            _output.WriteLine($"[PASS] PC2 validation error: {pc2Error}");

            // Go to Other Biological Parent tab and answer the question
            _output.WriteLine("\n[INFO] Navigating to Other Biological Parent tab");
            ClickAndVerifyTab(driver, "OBP", "Other Biological Parent");
            _output.WriteLine("[PASS] Other Biological Parent tab is now active");

            // Always select Yes first to test the complete OBP flow
            _output.WriteLine("[INFO] Selecting 'Yes' for 'Does OBP live in the Household at Enrollment?'");
            SelectSpecificRadioButton(driver, "OBPLiveInHouse", true);
            _output.WriteLine("[PASS] Selected Yes for OBP household question");

            // Go back to Informed Consent tab and submit again
            _output.WriteLine("\n[INFO] Returning to Informed Consent tab");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            _output.WriteLine("[INFO] Submitting form - expecting 'OBP is missing' validation error");
            ClickSubmitButtonOnInformedConsentTab(driver);
            Thread.Sleep(2000);

            // Check validation errors - should see "OBP is missing" and PC2 error
            validationErrors = GetAllValidationErrors(driver);
            _output.WriteLine($"[INFO] Found {validationErrors.Count} validation error(s) after selecting Yes for OBP");
            
            var obpMissingError = validationErrors.FirstOrDefault(e => 
                e.Contains("OBP is missing", StringComparison.OrdinalIgnoreCase));
            pc2Error = validationErrors.FirstOrDefault(e => 
                e.Contains("PC2", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("Primary Caregiver 2", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(obpMissingError);
            Assert.NotNull(pc2Error);
            _output.WriteLine($"[PASS] OBP is missing error present: {obpMissingError}");
            _output.WriteLine($"[PASS] PC2 validation error still present: {pc2Error}");

            // Need to assign an OBP
            _output.WriteLine("\n[INFO] Need to assign OBP - navigating back to OBP tab");
            ClickAndVerifyTab(driver, "OBP", "Other Biological Parent");
            
            // Click "Assign Other Biological Parent" button and search for "spider"
            AssignOtherBiologicalParent(driver, "spider");
            _output.WriteLine("[PASS] Successfully assigned OBP (spider)");

            // Go back to Informed Consent tab
            _output.WriteLine("\n[INFO] Returning to Informed Consent tab after assigning OBP");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            
            // Check if confidentiality agreement needs to be selected again (might have been reset)
            _output.WriteLine("[INFO] Checking if confidentiality agreement needs to be reselected");
            ClickSubmitButtonOnInformedConsentTab(driver);
            Thread.Sleep(2000);

            // Check if confidentiality agreement validation appears
            validationErrors = GetAllValidationErrors(driver);
            var confidentialityError = validationErrors.FirstOrDefault(e => 
                e.Contains("22", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("confidentiality agreement", StringComparison.OrdinalIgnoreCase));

            if (confidentialityError != null)
            {
                _output.WriteLine($"[INFO] Confidentiality agreement validation appeared: {confidentialityError}");
                
                // Navigate back to Informed Consent tab (Agreement tab) to access the dropdown
                _output.WriteLine("[INFO] Navigating back to Informed Consent tab to reselect confidentiality agreement");
                ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
                Thread.Sleep(500);
                
                _output.WriteLine("[INFO] Reselecting 'Yes' for confidentiality agreement");
                SelectConfidentialityAgreement(driver, "Yes");
                _output.WriteLine("[PASS] Reselected 'Yes' for confidentiality agreement");

                // Submit again after reselecting
                _output.WriteLine("[INFO] Submitting form again after reselecting confidentiality agreement");
                ClickSubmitButtonOnInformedConsentTab(driver);
                Thread.Sleep(2000);
            }
            else
            {
                _output.WriteLine("[INFO] Confidentiality agreement still valid, no need to reselect");
            }

            // Verify OBP missing error is gone, only PC2 error remains
            validationErrors = GetAllValidationErrors(driver);
            _output.WriteLine($"[INFO] Found {validationErrors.Count} validation error(s) after assigning OBP");
            
            obpMissingError = validationErrors.FirstOrDefault(e => 
                e.Contains("OBP is missing", StringComparison.OrdinalIgnoreCase));
            pc2Error = validationErrors.FirstOrDefault(e => 
                e.Contains("PC2", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("Primary Caregiver 2", StringComparison.OrdinalIgnoreCase));
            
            Assert.Null(obpMissingError);
            Assert.NotNull(pc2Error);
            _output.WriteLine("[PASS] OBP is missing error is gone after assigning OBP");
            _output.WriteLine($"[PASS] PC2 validation error still present: {pc2Error}");

            // Remove the OBP and select No instead
            _output.WriteLine("\n[INFO] Removing OBP and selecting No");
            ClickAndVerifyTab(driver, "OBP", "Other Biological Parent");
            RemoveOtherBiologicalParent(driver);
            _output.WriteLine("[PASS] Successfully removed OBP");

            // Select No for OBP household question
            SelectSpecificRadioButton(driver, "OBPLiveInHouse", false);
            _output.WriteLine("[PASS] Selected No for OBP household question");
            VerifyAssignButtonVisibility(driver, "Assign Other Biological Parent", shouldBeVisible: false);

            // Go to Primary Caregiver 2 tab and do the same flow as OBP
            _output.WriteLine("\n[INFO] Navigating to Primary Caregiver 2 tab");
            ClickAndVerifyTab(driver, "PC2", "Primary Caregiver 2");
            _output.WriteLine("[PASS] Primary Caregiver 2 tab is now active");

            // Always select Yes first to test the complete PC2 flow
            _output.WriteLine("[INFO] Selecting 'Yes' for 'Is there a PC2 in the home at enrollment?'");
            SelectSpecificRadioButton(driver, "PC2LiveInHouse", true);
            _output.WriteLine("[PASS] Selected Yes for PC2 household question");

            // Go back to Informed Consent tab and submit
            _output.WriteLine("\n[INFO] Returning to Informed Consent tab");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            _output.WriteLine("[INFO] Submitting form - checking validation");
            ClickSubmitButtonOnInformedConsentTab(driver);
            Thread.Sleep(2000);

            // Check if confidentiality agreement validation appears first
            validationErrors = GetAllValidationErrors(driver);
            confidentialityError = validationErrors.FirstOrDefault(e => 
                e.Contains("22", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("confidentiality agreement", StringComparison.OrdinalIgnoreCase));

            if (confidentialityError != null)
            {
                _output.WriteLine($"[INFO] Confidentiality agreement validation appeared: {confidentialityError}");
                
                // Navigate back to Informed Consent tab to access the dropdown
                _output.WriteLine("[INFO] Navigating back to Informed Consent tab to reselect confidentiality agreement");
                ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
                Thread.Sleep(500);
                
                _output.WriteLine("[INFO] Reselecting 'Yes' for confidentiality agreement");
                SelectConfidentialityAgreement(driver, "Yes");
                _output.WriteLine("[PASS] Reselected 'Yes' for confidentiality agreement");

                // Submit again after reselecting
                _output.WriteLine("[INFO] Submitting form again after reselecting confidentiality agreement");
                ClickSubmitButtonOnInformedConsentTab(driver);
                Thread.Sleep(2000);
            }

            // Now check for PC2 missing error
            validationErrors = GetAllValidationErrors(driver);
            var pc2MissingError = validationErrors.FirstOrDefault(e => 
                e.Contains("PC2 is missing", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(pc2MissingError);
            _output.WriteLine($"[PASS] PC2 is missing error present: {pc2MissingError}");

            // Need to assign a PC2
            _output.WriteLine("\n[INFO] Need to assign PC2 - navigating back to PC2 tab");
            ClickAndVerifyTab(driver, "PC2", "Primary Caregiver 2");
            
            // Click "Assign Primary Caregiver 2" button and search for "unit"
            AssignPrimaryCaregiver2(driver, "unit");
            _output.WriteLine("[PASS] Successfully assigned PC2 (unit)");

            // Go back to Informed Consent tab
            _output.WriteLine("\n[INFO] Returning to Informed Consent tab after assigning PC2");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            
            // Check if confidentiality agreement needs to be selected again
            _output.WriteLine("[INFO] Checking if confidentiality agreement needs to be reselected");
            ClickSubmitButtonOnInformedConsentTab(driver);
            Thread.Sleep(2000);

            // Check if confidentiality agreement validation appears
            validationErrors = GetAllValidationErrors(driver);
            confidentialityError = validationErrors.FirstOrDefault(e => 
                e.Contains("22", StringComparison.OrdinalIgnoreCase) && 
                e.Contains("confidentiality agreement", StringComparison.OrdinalIgnoreCase));

            if (confidentialityError != null)
            {
                _output.WriteLine($"[INFO] Confidentiality agreement validation appeared: {confidentialityError}");
                
                // Navigate back to Informed Consent tab to access the dropdown
                _output.WriteLine("[INFO] Navigating back to Informed Consent tab to reselect confidentiality agreement");
                ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
                Thread.Sleep(500);
                
                _output.WriteLine("[INFO] Reselecting 'Yes' for confidentiality agreement");
                SelectConfidentialityAgreement(driver, "Yes");
                _output.WriteLine("[PASS] Reselected 'Yes' for confidentiality agreement");

                // Submit again after reselecting
                _output.WriteLine("[INFO] Submitting form again after reselecting confidentiality agreement");
                ClickSubmitButtonOnInformedConsentTab(driver);
                Thread.Sleep(2000);
            }
            else
            {
                _output.WriteLine("[INFO] Confidentiality agreement still valid, no need to reselect");
            }

            // Verify PC2 missing error is gone
            validationErrors = GetAllValidationErrors(driver);
            pc2MissingError = validationErrors.FirstOrDefault(e => 
                e.Contains("PC2 is missing", StringComparison.OrdinalIgnoreCase));
            
            Assert.Null(pc2MissingError);
            _output.WriteLine("[PASS] PC2 is missing error is gone after assigning PC2");

            // Remove the PC2 and select No instead
            _output.WriteLine("\n[INFO] Removing PC2 and selecting No");
            ClickAndVerifyTab(driver, "PC2", "Primary Caregiver 2");
            RemovePrimaryCaregiver2(driver);
            _output.WriteLine("[PASS] Successfully removed PC2");

            // Select No for PC2 household question
            SelectSpecificRadioButton(driver, "PC2LiveInHouse", false);
            _output.WriteLine("[PASS] Selected No for PC2 household question");
            VerifyAssignButtonVisibility(driver, "Assign Primary Caregiver 2", shouldBeVisible: false);

            // Go back to Informed Consent tab and submit - should succeed now
            _output.WriteLine("\n[INFO] Returning to Informed Consent tab for final submission");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            _output.WriteLine("[INFO] Submitting form - expecting success with toast message");
            ClickSubmitButtonOnInformedConsentTab(driver);
            Thread.Sleep(2000);

            // Verify success toast message - look for the toast heading specifically
            var toastHeading = driver.FindElements(By.CssSelector(
                ".jq-toast-heading, " +
                "h2.jq-toast-heading"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            if (toastHeading != null)
            {
                var headingText = toastHeading.Text.Trim();
                _output.WriteLine($"[INFO] Toast heading: {headingText}");
                Assert.Contains("Form Saved", headingText, StringComparison.OrdinalIgnoreCase);
                _output.WriteLine("[PASS] Form saved successfully - toast heading confirmed");

                // Also verify the PC1 ID is in the toast message body
                var toastBody = driver.FindElements(By.CssSelector(".jq-toast-single"))
                    .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));
                
                if (toastBody != null)
                {
                    var bodyText = toastBody.Text;
                    _output.WriteLine($"[INFO] Toast body contains PC1 ID: {bodyText.Contains(pc1Id, StringComparison.OrdinalIgnoreCase)}");
                    Assert.Contains(pc1Id, bodyText, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("Identification and Contact Information", bodyText, StringComparison.OrdinalIgnoreCase);
                    _output.WriteLine("[PASS] Toast message confirmed form type and PC1 ID");
                }
            }
            else
            {
                // Fallback: verify no validation errors remain
                validationErrors = GetAllValidationErrors(driver);
                _output.WriteLine($"[INFO] No toast found. Found {validationErrors.Count} validation error(s)");
                Assert.Empty(validationErrors);
                _output.WriteLine("[PASS] Form saved successfully - no validation errors present");
            }

            _output.WriteLine("\n[PASS] Successfully submitted Identification and Contact Information form after completing all required fields!");
        }

        /// <summary>
        /// Tests editing an already-submitted Identification and Contact Information form
        /// by opening it and saving without making changes.
        /// 
        /// NOTE: This test expects the form to have been previously submitted (e.g., by Test 4).
        /// It validates that the form can be opened and saved again successfully.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(5)]
        public void CheckingEditingAlreadySubmittedForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            // Use common helper for the navigation flow
            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            // Navigate to Identification and Contact Information (already submitted form)
            NavigateToIdContactInformation(driver, formsPane, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Identification and Contact Information page");

            // Verify we're on the form page
            var currentUrl = driver.Url;
            Assert.Contains("IdContactInformation.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] On form page: {currentUrl}");

            // Verify only Signed MIS Contact field is editable on Informed Consent tab
            _output.WriteLine("[INFO] Verifying edit permissions on Informed Consent tab");
            ClickAndVerifyTab(driver, "Agreement", "Informed Consent");
            VerifyInformedConsentEditPermissions(driver);
            _output.WriteLine("[PASS] Informed Consent tab enforces correct edit permissions");
            ClickAndVerifyTab(driver, "PC1", "Primary Caregiver 1");

            // Click Submit button without making any changes (testing edit of existing form)
            _output.WriteLine("\n[INFO] Submitting already-submitted form without changes");
            var submitButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-primary[title*='Save changes'], " +
                "a.btn.btn-primary .glyphicon-save"))
                .Select(el => el.TagName == "span" ? el.FindElement(By.XPath("./parent::a")) : el)
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found on the form page.");

            _output.WriteLine($"[INFO] Clicking Submit button: {submitButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify success toast message
            var toastHeading = driver.FindElements(By.CssSelector(
                ".jq-toast-heading, " +
                "h2.jq-toast-heading"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            Assert.NotNull(toastHeading);
            var headingText = toastHeading.Text.Trim();
            _output.WriteLine($"[INFO] Toast heading: {headingText}");
            Assert.Contains("Form Saved", headingText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Form saved successfully - toast heading confirmed");

            // Verify the PC1 ID is in the toast message body
            var toastBody = driver.FindElements(By.CssSelector(".jq-toast-single"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));
            
            Assert.NotNull(toastBody);
            var bodyText = toastBody.Text;
            _output.WriteLine($"[INFO] Toast message: {bodyText}");
            Assert.Contains(pc1Id, bodyText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Identification and Contact Information", bodyText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Toast message confirmed form type and PC1 ID");

            _output.WriteLine("\n[PASS] Successfully edited and saved already-submitted Identification and Contact Information form!");
        }

        #region Helper Methods

        /// <summary>
        /// Navigates to the Identification and Contact Information page from the forms pane
        /// </summary>
        private void NavigateToIdContactInformation(IPookieWebDriver driver, IWebElement formsPane, string pc1Id)
        {
            // Find the Identification and Contact Information link
            // Using CSS classes and semantic attributes, avoiding ASP.NET generated IDs
            var idContactLink = formsPane.FindElements(By.CssSelector(
                "a.list-group-item.moreInfo[href*='IdContactInformation.aspx'], " +
                "a.moreInfo[data-formtype='idc'], " +
                "a.list-group-item[title='Identification and Contact Information']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Identification and Contact Information link was not found inside the Forms tab.");

            _output.WriteLine($"Found Identification and Contact Information link: {idContactLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, idContactLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify we're on the IdContactInformation page
            var currentUrl = driver.Url;
            Assert.Contains("IdContactInformation.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Identification and Contact Information page opened successfully: {currentUrl}");
        }

        /// <summary>
        /// Clicks the "Edit Primary Caregiver 1" button, verifies edit page opens, and submits the form
        /// </summary>
        private void ClickEditPrimaryCaregiver1Button(IPookieWebDriver driver)
        {
            // Find the "Edit Primary Caregiver 1" button using CSS classes and semantic attributes
            var editButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-default .glyphicon-copy"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Edit Primary Caregiver 1", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Edit Primary Caregiver 1 button was not found.");

            _output.WriteLine($"[INFO] Clicking button: {editButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, editButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            // Verify we're on the PCProfile.aspx edit page
            var currentUrl = driver.Url;
            Assert.Contains("PCProfile.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] PCProfile edit page opened: {currentUrl}");

            // Click Submit button to save and return to IdContactInformation page
            var submitButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-primary .glyphicon-save"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found on PCProfile page.");

            _output.WriteLine($"[INFO] Clicking Submit button: {submitButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            // Verify we're back on the IdContactInformation page
            var returnUrl = driver.Url;
            Assert.Contains("IdContactInformation.aspx", returnUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Returned to IdContactInformation page: {returnUrl}");
        }

        /// <summary>
        /// Clicks a tab and verifies it becomes active
        /// </summary>
        private void ClickAndVerifyTab(IPookieWebDriver driver, string tabHref, string tabTitle)
        {
            // Find the tab link using CSS classes and attributes (not IDs)
            var tabLink = driver.FindElements(By.CssSelector(
                $"ul.nav.nav-pills li.nav-item a[href='#{tabHref}'][data-toggle='pill'], " +
                $"ul.nav.nav-pills li.nav-item a[title='{tabTitle}'][data-toggle='pill']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Tab link for '{tabTitle}' was not found.");

            _output.WriteLine($"[INFO] Clicking tab: {tabLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, tabLink);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            // Verify the tab's parent li has 'active' class
            var parentLi = tabLink.FindElement(By.XPath("./parent::li"));
            var parentClass = parentLi.GetAttribute("class") ?? string.Empty;
            Assert.Contains("active", parentClass, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[INFO] Tab '{tabTitle}' is now active");
        }

        /// <summary>
        /// Clicks the Submit button on the Informed Consent tab
        /// </summary>
        private void ClickSubmitButtonOnInformedConsentTab(IPookieWebDriver driver)
        {
            // Find the Submit button using CSS classes and semantic attributes (not ASP.NET IDs)
            var submitButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-primary[title*='Save changes'], " +
                "a.btn.btn-primary .glyphicon-save"))
                .Select(el => el.TagName == "span" ? el.FindElement(By.XPath("./parent::a")) : el)
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found on the Informed Consent tab.");

            _output.WriteLine($"[INFO] Clicking Submit button: {submitButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);
        }

        private IWebElement GetConfidentialityDropdown(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector("select.form-control"))
                .FirstOrDefault(el => el.Displayed &&
                    el.FindElements(By.CssSelector("option[value='1']")).Any() &&
                    el.FindElements(By.CssSelector("option[value='0']")).Any())
                ?? throw new InvalidOperationException("Signed Confidentiality Agreement dropdown was not found.");
        }

        /// <summary>
        /// Selects Yes/No for the Signed Confidentiality Agreement dropdown (Question 22)
        /// </summary>
        private void SelectConfidentialityAgreement(IPookieWebDriver driver, string answer)
        {
            var selectElement = new SelectElement(GetConfidentialityDropdown(driver));
            
            // Select based on the answer (Yes = 1, No = 0)
            if (answer.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                WebElementHelper.SelectByTextOrValue(selectElement, "Yes", "1");
            }
            else if (answer.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                WebElementHelper.SelectByTextOrValue(selectElement, "No", "0");
            }
            else
            {
                throw new ArgumentException($"Invalid answer '{answer}'. Must be 'Yes' or 'No'.");
            }

            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);
            
            _output.WriteLine($"[INFO] Selected '{answer}' for Signed Confidentiality Agreement");
        }

        /// <summary>
        /// Clears the Signed Confidentiality Agreement selection (sets it to the placeholder option)
        /// </summary>
        private void ClearConfidentialityAgreementSelection(IPookieWebDriver driver)
        {
            var selectElement = new SelectElement(GetConfidentialityDropdown(driver));

            var placeholderOption = selectElement.Options.FirstOrDefault(opt =>
                string.IsNullOrWhiteSpace(opt.GetAttribute("value")) ||
                opt.Text.Contains("select", StringComparison.OrdinalIgnoreCase));

            if (placeholderOption == null)
            {
                throw new InvalidOperationException("Placeholder option for Signed Confidentiality Agreement was not found.");
            }

            WebElementHelper.SelectByTextOrValue(
                selectElement,
                placeholderOption.Text.Trim(),
                placeholderOption.GetAttribute("value"));

            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            _output.WriteLine("[INFO] Cleared Signed Confidentiality Agreement selection");
        }

        /// <summary>
        /// Gets all validation error messages from the page
        /// </summary>
        private List<string> GetAllValidationErrors(IPookieWebDriver driver)
        {
            var validationElements = driver.FindElements(By.CssSelector(
                "span[style*='color:Red'], " +
                "span[style*='color: red'], " +
                ".text-danger, " +
                "span.text-danger, " +
                "div.alert.alert-danger, " +
                "div.validation-summary-errors"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .ToList();

            var errors = new List<string>();
            foreach (var element in validationElements)
            {
                var text = element.Text.Trim();
                errors.Add(text);
                _output.WriteLine($"[DEBUG] Validation error: {text}");
            }

            return errors;
        }

        /// <summary>
        /// Selects a random Yes/No radio button for a given radio group name
        /// Returns true if "Yes" was selected, false if "No" was selected
        /// </summary>
        private bool SelectRandomRadioButton(IPookieWebDriver driver, string radioGroupName)
        {
            // Find radio buttons by name attribute using partial match (avoids ASP.NET ID prefixes)
            var radioButtons = driver.FindElements(By.CssSelector(
                $"input[type='radio'][name*='{radioGroupName}']"))
                .Where(el => el.Displayed)
                .ToList();

            if (radioButtons.Count < 2)
            {
                throw new InvalidOperationException($"Expected at least 2 radio buttons for group '{radioGroupName}', but found {radioButtons.Count}");
            }

            // Randomly select Yes or No (0 or 1)
            var random = new Random();
            var selectedButton = radioButtons[random.Next(radioButtons.Count)];
            
            // Get the label text to know what we're selecting
            var labelFor = selectedButton.GetAttribute("id");
            var label = driver.FindElements(By.CssSelector($"label[for='{labelFor}']"))
                .FirstOrDefault();
            var labelText = label?.Text ?? "Unknown";

            _output.WriteLine($"[INFO] Randomly selected: {labelText}");
            CommonTestHelper.ClickElement(driver, selectedButton);
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);

            // Return true if Yes was selected
            return labelText.Equals("Yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Selects a specific Yes/No radio button for a given radio group name
        /// </summary>
        private void SelectSpecificRadioButton(IPookieWebDriver driver, string radioGroupName, bool selectYes)
        {
            // Find radio buttons by name attribute using partial match
            var radioButtons = driver.FindElements(By.CssSelector(
                $"input[type='radio'][name*='{radioGroupName}']"))
                .Where(el => el.Displayed)
                .ToList();

            if (radioButtons.Count < 2)
            {
                throw new InvalidOperationException($"Expected at least 2 radio buttons for group '{radioGroupName}', but found {radioButtons.Count}");
            }

            // Find the Yes or No button based on the parameter
            foreach (var button in radioButtons)
            {
                var labelFor = button.GetAttribute("id");
                var label = driver.FindElements(By.CssSelector($"label[for='{labelFor}']"))
                    .FirstOrDefault();
                var labelText = label?.Text ?? "";

                bool isYesButton = labelText.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                bool isNoButton = labelText.Equals("No", StringComparison.OrdinalIgnoreCase);

                if ((selectYes && isYesButton) || (!selectYes && isNoButton))
                {
                    _output.WriteLine($"[INFO] Selecting: {labelText}");
                    CommonTestHelper.ClickElement(driver, button);
                    driver.WaitForUpdatePanel(10);
                    driver.WaitForReady(10);
                    Thread.Sleep(500);
                    return;
                }
            }

            throw new InvalidOperationException($"Could not find {(selectYes ? "Yes" : "No")} button for group '{radioGroupName}'");
        }

        /// <summary>
        /// Verifies whether an assign button remains visible or hidden after selecting Yes/No.
        /// </summary>
        private void VerifyAssignButtonVisibility(IPookieWebDriver driver, string buttonText, bool shouldBeVisible)
        {
            driver.WaitForReady(5);
            Thread.Sleep(500);

            var buttons = driver.FindElements(By.CssSelector(
                    "a.btn.btn-default .glyphicon-copy"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .Where(btn => (btn.Text ?? string.Empty).Contains(buttonText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (shouldBeVisible)
            {
                var visibleButton = buttons.FirstOrDefault(btn => btn.Displayed);
                Assert.NotNull(visibleButton);
                _output.WriteLine($"[PASS] '{buttonText}' button is visible as expected.");
                return;
            }

            if (buttons.Count == 0)
            {
                _output.WriteLine($"[PASS] '{buttonText}' button removed from DOM as expected when hidden.");
                return;
            }

            var anyDisplayed = buttons.Any(btn => btn.Displayed);
            Assert.False(anyDisplayed, $"'{buttonText}' button should be hidden after selecting 'No'.");
            _output.WriteLine($"[PASS] '{buttonText}' button is hidden as expected.");
        }

        /// <summary>
        /// Assigns an Other Biological Parent by searching and selecting
        /// </summary>
        private void AssignOtherBiologicalParent(IPookieWebDriver driver, string firstName)
        {
            // Click "Assign Other Biological Parent" button
            var assignButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-default .glyphicon-copy"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Assign Other Biological Parent", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Assign Other Biological Parent button was not found.");

            _output.WriteLine($"[INFO] Clicking: {assignButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, assignButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            // Enter first name in search field
            var firstNameInput = driver.FindElements(By.CssSelector("input.form-control[type='text']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("OBP First Name input was not found.");

            firstNameInput.Clear();
            firstNameInput.SendKeys(firstName);
            _output.WriteLine($"[INFO] Entered '{firstName}' in search field");

            // Click Search button
            var searchButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-primary .glyphicon-search"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Search", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Search button was not found.");

            _output.WriteLine("[INFO] Clicking Search button");
            CommonTestHelper.ClickElement(driver, searchButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            // Click Select link in the results
            var selectLink = driver.FindElements(By.TagName("a"))
                .FirstOrDefault(el => el.Displayed && el.Text.Equals("Select", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Select link was not found in search results.");

            _output.WriteLine("[INFO] Clicking Select link");
            CommonTestHelper.ClickElement(driver, selectLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);
        }

        /// <summary>
        /// Assigns a Primary Caregiver 2 by searching and selecting
        /// </summary>
        private void AssignPrimaryCaregiver2(IPookieWebDriver driver, string firstName)
        {
            // Click "Assign Primary Caregiver 2" button
            var assignButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-default .glyphicon-copy"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Assign Primary Caregiver 2", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Assign Primary Caregiver 2 button was not found.");

            _output.WriteLine($"[INFO] Clicking: {assignButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, assignButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            // Enter first name in search field
            var firstNameInput = driver.FindElements(By.CssSelector("input.form-control[type='text']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PC2 First Name input was not found.");

            firstNameInput.Clear();
            firstNameInput.SendKeys(firstName);
            _output.WriteLine($"[INFO] Entered '{firstName}' in search field");

            // Click Search button
            var searchButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-primary .glyphicon-search"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Search", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Search button was not found.");

            _output.WriteLine("[INFO] Clicking Search button");
            CommonTestHelper.ClickElement(driver, searchButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            // Click Select link in the results
            var selectLink = driver.FindElements(By.TagName("a"))
                .FirstOrDefault(el => el.Displayed && el.Text.Equals("Select", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Select link was not found in search results.");

            _output.WriteLine("[INFO] Clicking Select link");
            CommonTestHelper.ClickElement(driver, selectLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);
        }

        /// <summary>
        /// Removes the Primary Caregiver 2 and verifies the toast message
        /// </summary>
        private void RemovePrimaryCaregiver2(IPookieWebDriver driver)
        {
            // Click "Remove Primary Caregiver 2" button
            var removeButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-danger .glyphicon-remove-sign"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Remove Primary Caregiver 2", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Remove Primary Caregiver 2 button was not found.");

            _output.WriteLine($"[INFO] Clicking: {removeButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, removeButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify toast message - look for the toast heading specifically
            var toastHeading = driver.FindElements(By.CssSelector(
                ".jq-toast-heading, " +
                "h2.jq-toast-heading"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            if (toastHeading != null)
            {
                var headingText = toastHeading.Text.Trim();
                _output.WriteLine($"[INFO] Toast heading: {headingText}");
                Assert.Contains("PC2 Removed", headingText, StringComparison.OrdinalIgnoreCase);
                _output.WriteLine("[PASS] PC2 removed successfully - toast message confirmed");
            }
            else
            {
                // Fallback to general toast message check
                var toastMessage = WebElementHelper.GetToastMessage(driver, 1000);
                Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Remove toast message was not displayed.");
                _output.WriteLine($"[INFO] Toast message: {toastMessage}");
                Assert.True(
                    toastMessage.Contains("PC2 Removed", StringComparison.OrdinalIgnoreCase) ||
                    toastMessage.Contains("removed", StringComparison.OrdinalIgnoreCase),
                    "Toast message did not confirm PC2 removal");
                _output.WriteLine("[PASS] PC2 removed successfully - toast message confirmed");
            }
        }

        /// <summary>
        /// Removes the Other Biological Parent and verifies the toast message
        /// </summary>
        private void RemoveOtherBiologicalParent(IPookieWebDriver driver)
        {
            // Click "Remove Other Biological Parent" button
            var removeButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-danger .glyphicon-remove-sign"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Remove Other Biological Parent", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Remove Other Biological Parent button was not found.");

            _output.WriteLine($"[INFO] Clicking: {removeButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, removeButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(2000);

            // Verify toast message - look for the toast heading specifically
            var toastHeading = driver.FindElements(By.CssSelector(
                ".jq-toast-heading, " +
                "h2.jq-toast-heading"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            if (toastHeading != null)
            {
                var headingText = toastHeading.Text.Trim();
                _output.WriteLine($"[INFO] Toast heading: {headingText}");
                Assert.Contains("OBP Removed", headingText, StringComparison.OrdinalIgnoreCase);
                _output.WriteLine("[PASS] OBP removed successfully - toast message confirmed");
            }
            else
            {
                // Fallback to general toast message check
                var toastMessage = WebElementHelper.GetToastMessage(driver, 1000);
                Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Remove toast message was not displayed.");
                _output.WriteLine($"[INFO] Toast message: {toastMessage}");
                Assert.True(
                    toastMessage.Contains("OBP Removed", StringComparison.OrdinalIgnoreCase) ||
                    toastMessage.Contains("removed", StringComparison.OrdinalIgnoreCase),
                    "Toast message did not confirm OBP removal");
                _output.WriteLine("[PASS] OBP removed successfully - toast message confirmed");
            }
        }

        /// <summary>
        /// Verifies the Primary Caregiver 1 tab content
        /// </summary>
        private void VerifyPrimaryCaregiver1Tab(IPookieWebDriver driver, string pc1Id)
        {
            // Verify the alert message is present
            var alertMessage = driver.FindElements(By.CssSelector(
                ".alert.alert-success, " +
                "div.alert-success"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Primary Caregiver 1", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(alertMessage);
            _output.WriteLine($"[INFO] Found alert message: {alertMessage.Text.Trim().Substring(0, Math.Min(50, alertMessage.Text.Trim().Length))}...");

            // Verify PC1 ID is displayed
            var pc1IdLabel = driver.FindElements(By.CssSelector(
                "span.replaceBlank"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains(pc1Id, StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(pc1IdLabel);
            _output.WriteLine($"[INFO] PC1 ID displayed: {pc1IdLabel.Text.Trim()}");

            // Verify Home Visitor dropdown is present (will be disabled in view mode after submit)
            var homeVisitorDropdown = driver.FindElements(By.CssSelector(
                "select.form-control.replaceBlank, " +
                "select.form-control"))
                .FirstOrDefault(el => el.Displayed);

            Assert.NotNull(homeVisitorDropdown);
            _output.WriteLine($"[INFO] Home Visitor dropdown is present");

            // Verify labels are present (Name, Address, Phone, Email)
            var labels = new[] { "Name", "Address", "Primary Phone", "Email Address" };
            foreach (var labelText in labels)
            {
                var label = driver.FindElements(By.TagName("label"))
                    .FirstOrDefault(el => el.Displayed && el.Text.Contains(labelText, StringComparison.OrdinalIgnoreCase));

                Assert.NotNull(label);
                _output.WriteLine($"[INFO] Found label: {labelText}");
            }
        }

        /// <summary>
        /// Verifies the Other Biological Parent tab content
        /// </summary>
        private void VerifyOtherBiologicalParentTab(IPookieWebDriver driver)
        {
            // Verify the alert message is present
            var alertMessage = driver.FindElements(By.CssSelector(
                ".alert.alert-success, " +
                "div.alert-success"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Other Biological Parent", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(alertMessage);
            _output.WriteLine($"[INFO] Found alert message for OBP tab");

            // Verify the "Does OBP live in the Household at Enrollment?" question
            var obpQuestion = driver.FindElements(By.TagName("label"))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Does OBP live in the Household", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(obpQuestion);
            _output.WriteLine($"[INFO] Found question: {obpQuestion.Text.Trim()}");

            // Verify radio buttons are present (Yes/No)
            var radioButtons = driver.FindElements(By.CssSelector(
                "input[type='radio']"))
                .Where(el => el.Displayed)
                .ToList();

            Assert.True(radioButtons.Count >= 2, "Expected at least 2 radio buttons for Yes/No");
            _output.WriteLine($"[INFO] Found {radioButtons.Count} radio buttons");

            // Verify "Assign Other Biological Parent" button is present
            var assignButton = driver.FindElements(By.CssSelector(
                "a.btn.btn-default .glyphicon-copy"))
                .Select(icon => icon.FindElement(By.XPath("./parent::a")))
                .FirstOrDefault(btn => btn.Displayed && btn.Text.Contains("Assign Other Biological Parent", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(assignButton);
            _output.WriteLine($"[INFO] Found button: {assignButton.Text?.Trim()}");

            // Verify OBP labels are present
            var labels = new[] { "OBP's Name", "OBP's Birth Date", "Other Biological Parent's Address", "Other Biological Parent's Phone" };
            foreach (var labelText in labels)
            {
                var label = driver.FindElements(By.TagName("label"))
                    .FirstOrDefault(el => el.Displayed && el.Text.Contains(labelText, StringComparison.OrdinalIgnoreCase));

                Assert.NotNull(label);
                _output.WriteLine($"[INFO] Found label: {labelText}");
            }
        }

        /// <summary>
        /// Verifies the Primary Caregiver 2 tab content
        /// </summary>
        private void VerifyPrimaryCaregiver2Tab(IPookieWebDriver driver)
        {
            // Verify panel body is present and visible
            var panelBody = driver.FindElements(By.CssSelector(".panel-body"))
                .FirstOrDefault(el => el.Displayed);

            Assert.NotNull(panelBody);
            _output.WriteLine("[INFO] Primary Caregiver 2 tab content is visible");

            // Verify some content is present (could be similar to OBP or PC1)
            var labels = driver.FindElements(By.TagName("label"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .ToList();

            Assert.True(labels.Count > 0, "Expected to find labels in Primary Caregiver 2 tab");
            _output.WriteLine($"[INFO] Found {labels.Count} labels in Primary Caregiver 2 tab");
        }

        /// <summary>
        /// Verifies the Emergency Contact Person tab content
        /// </summary>
        private void VerifyEmergencyContactTab(IPookieWebDriver driver)
        {
            // Verify panel body is present and visible
            var panelBody = driver.FindElements(By.CssSelector(".panel-body"))
                .FirstOrDefault(el => el.Displayed);

            Assert.NotNull(panelBody);
            _output.WriteLine("[INFO] Emergency Contact Person tab content is visible");

            // Verify some content is present
            var labels = driver.FindElements(By.TagName("label"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .ToList();

            Assert.True(labels.Count > 0, "Expected to find labels in Emergency Contact Person tab");
            _output.WriteLine($"[INFO] Found {labels.Count} labels in Emergency Contact Person tab");
        }

        /// <summary>
        /// Verifies the Informed Consent tab content
        /// </summary>
        private void VerifyInformedConsentTab(IPookieWebDriver driver)
        {
            // Verify panel body is present and visible
            var panelBody = driver.FindElements(By.CssSelector(".panel-body"))
                .FirstOrDefault(el => el.Displayed);

            Assert.NotNull(panelBody);
            _output.WriteLine("[INFO] Informed Consent tab content is visible");

            // Verify some content is present (could be text content, checkboxes, etc.)
            var contentElements = driver.FindElements(By.CssSelector(
                ".panel-body label, " +
                ".panel-body p, " +
                ".panel-body input, " +
                ".panel-body div"))
                .Where(el => el.Displayed)
                .ToList();

            Assert.True(contentElements.Count > 0, "Expected to find content in Informed Consent tab");
            _output.WriteLine($"[INFO] Found {contentElements.Count} content elements in Informed Consent tab");
        }

        /// <summary>
        /// Ensures that only the Signed MIS Contact dropdown remains editable on the Informed Consent tab.
        /// </summary>
        private void VerifyInformedConsentEditPermissions(IPookieWebDriver driver)
        {
            var consentTab = driver.FindElements(By.CssSelector("div.tab-pane.active"))
                .FirstOrDefault(el =>
                {
                    var id = el.GetAttribute("id") ?? string.Empty;
                    return id.Contains("Agreement", StringComparison.OrdinalIgnoreCase);
                })
                ?? driver.FindElements(By.CssSelector("div.tab-pane.active"))
                    .FirstOrDefault()
                ?? throw new InvalidOperationException("Informed Consent tab content was not found.");

            var misContactDateInput = consentTab.FindElements(By.CssSelector(".input-group.date input.form-control"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Signed MIS contact date input was not found on the Informed Consent tab.");

            Assert.False(misContactDateInput.Enabled, "Signed MIS contact date input should be read-only.");
            var disabledAttr = misContactDateInput.GetAttribute("disabled") ?? string.Empty;
            Assert.True(!string.IsNullOrWhiteSpace(disabledAttr), "Signed MIS contact date input should have the disabled attribute set.");
            _output.WriteLine("[INFO] Signed MIS contact date input is disabled as expected.");

            var signedConfidentialityDropdown = GetConfidentialityDropdown(driver);

            Assert.True(signedConfidentialityDropdown.Enabled, "Signed MIS contact dropdown should remain editable.");
            var selectElement = new SelectElement(signedConfidentialityDropdown);
            var selectedText = selectElement.SelectedOption?.Text?.Trim() ?? string.Empty;
            _output.WriteLine($"[INFO] Signed MIS contact dropdown is editable. Current selection: {selectedText}");
        }

        #endregion
    }
}

