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

        #endregion
    }
}

