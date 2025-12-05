using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.Pages;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.TargetChildID
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public partial class TargetChildIDTests : IClassFixture<AppConfig>
    {
        private const string TargetChildLinkSelector =
            "a.list-group-item.moreInfo[href*='TCIDs.aspx'], " +
            "a.moreInfo[data-formtype='TCIBO'], " +
            "a.list-group-item[title*='Target Child Information']";
        private const string InfoAlertSelector = "div.alert.alert-info";
        private const string NewTcidButtonSelector =
            "div.panel-heading a.btn.btn-default.pull-right[title*='New'], " +
            "div.panel-heading a.btn.btn-default[href*='btnSubmit'], " +
            "a.btn.btn-default[href*='btnSubmit']";
        private const string SubmitButtonSelector =
            "div.panel-footer a.btn.btn-primary, " +
            "a.btn.btn-primary[title*='Save'], " +
            "a.btn.btn-primary[href*='btnSubmit']";
        private const string ValidationSummarySelector =
            "div[id$='ValidationSummary1'], div.validation-summary, ul.validation-summary-errors";
        private const string BirthTermDropdownSelector = "select.form-control[id$='ddlBirthTerm']";
        private const string FirstNameInputSelector = "input.form-control[id$='txtTCFirstName']";
        private const string LastNameInputSelector = "input.form-control[id$='txtTCLastName']";
        private const string ParityDropdownSelector = "select.form-control[id$='ddlParity']";
        private const string GestationalAgeInputSelector = "input.form-control[id$='txtGestationalAge']";
        private const string BirthWeightLbsInputSelector = "input.form-control[id$='txtBirthWtLbs']";
        private const string BirthWeightOzInputSelector = "input.form-control[id$='txtBirthWtOz']";
        private const string MedicaidDropdownSelector = "select.form-control[id$='ddlTCReceivingMedicaid']";
        private const string HealthInsuranceCheckboxesSelector =
            "input[id$='chkTCHIFamilyChildHealth'], " +
            "input[id$='chkTCHIPrivateInsurance'], " +
            "input[id$='chkTCHIOther'], " +
            "input[id$='chkTCHIUninsured'], " +
            "input[id$='chkTCHIUnknown']";
        private const string HealthInsuranceOtherCheckboxSelector = "input[id$='chkTCHIOther']";
        private const string HealthInsuranceOtherSpecifyInputSelector = "input.form-control[id$='txtTCHIOtherSpecify']";
        private const string HasMedicalProviderDropdownSelector = "select.form-control[id$='ddlTCHasMedicalProvider']";
        private const string MedicalProviderDropdownSelector = "select.form-control[id$='ddlTCMedicalProviderFK']";
        private const string MedicalFacilityDropdownSelector = "select.form-control[id$='ddlTCMedicalFacilityFK']";
        private const string MedicalProviderNotInListLinkSelector = "a[id$='lnkNewMedicalProvider']";
        private const string MedicalProviderModalSelector = ".modal.show .modal-content, .modal.fade.in .modal-content";
        private const string MedicalProviderModalValidationSelector = "div[id$='ctlMedicalProvider_vsMP']";
        private const string MedicalProviderModalSubmitButtonSelector = "a[id$='ctlMedicalProvider_btnSubmitProvider']";
        private const string MedicalProviderModalFirstNameInputSelector = "input[id$='ctlMedicalProvider_txtMPFirstName']";
        private const string MedicalProviderModalLastNameInputSelector = "input[id$='ctlMedicalProvider_txtMPLastName']";
        private const string MedicalProviderModalAddressInputSelector = "input[id$='ctlMedicalProvider_txtMPAddress']";
        private const string MedicalProviderModalStateInputSelector = "input[id$='ctlMedicalProvider_txtMPState']";
        private const string MedicalProviderModalZipInputSelector = "input[id$='ctlMedicalProvider_txtMPZip']";
        private const string MedicalProviderModalPhoneInputSelector = "input[id$='ctlMedicalProvider_txtMPPhone']";
        private const string MedicaidCaseNumberInputSelector = "input.form-control[id$='txtTcHIMedicaidCaseNumber']";
        private const string MedicalFacilityNotInListLinkSelector = "a[id$='lnkNewMedicalFacility']";
        private const string MedicalFacilityModalSelector = ".modal.show .modal-content, .modal.fade.in .modal-content";
        private const string MedicalFacilityModalValidationSelector = "div[id$='ctlMedicalFacility_vsMF']";
        private const string MedicalFacilityModalSubmitButtonSelector = "a[id$='ctlMedicalFacility_btnSubmitFacility']";
        private const string DeliveryTypeDropdownSelector = "select.form-control[id$='ddlDeliveryType']";
        private const string ChildFedBreastMilkDropdownSelector = "select.form-control[id$='ddlChildFedBreastMilk']";
        private const string AdditionalItemsTooltipSelector = "#OptionalItems span.glyphicon.glyphicon-question-sign";
        private const string AdditionalItemsTabSelector = "#OptionalItems";
        private const string PhqTabSelector = "#PHQ9";
        private const string PhqDateInputSelector = "input.form-control[id$='txtPHQDateAdministered']";
        private const string IntakeDateLabelSelector = "span[id$='lblIntakeDate']";
        private const string TargetChildDobLabelSelector = "span[id$='lblTCDOB']";
        private const string PhqParticipantDropdownSelector = "select.form-control[id$='ddlPHQ9Participant']";
        private const string PhqParticipantSpecifyInputSelector = "input.form-control[id$='txtPHQ9ParticipantSpecify']";
        private const string PhqRefusedCheckboxSelector = "input[id$='chkPHQ9Refused']";
        private const string PhqWorkerDropdownSelector = "select.form-control[id$='ddlPHQWorker']";
        private const string PhqScoreLabelSelector = "span[id$='lblPHQ9Score']";
        private const string PhqResultLabelSelector = "span[id$='lblPHQ9Result']";
        private const string PhqScoreValidityLabelSelector = "span[id$='lblPHQ9ScoreValidity']";
        private const string PhqQuestionDropdownsSelector = "#PHQ9 select.phq9score";
        private const string PhqDifficultyDropdownSelector = "select.form-control[id$='ddlDifficulty']";
        private const string PhqDepressionReferralCheckboxSelector = "input[id$='chkDepressionReferralMade']";
        private const string MiechvTabSelector = "#MIECHV";
        private const string MedicalCareSourceDropdownSelector = "select.form-control[id$='ddlTCMedicalCareSource']";
        private const string MedicalCareSourceSpecifyInputSelector = "input.form-control[id$='txtTCMedicalCareSourceOtherSpecify']";
        private const string PrenatalCareInputSelector =
            "input.form-control[id*='Prenatal'][type='text'], " +
            "input.form-control[name*='Prenatal'], " +
            "input.form-control[id*='txtFirstPrenatal'], " +
            "input.form-control[id*='txtPrenatalCare']";
        private const string TcidGridSelector = "table.table.table-condensed[id*='grTCIDs'], table[id*='grTCIDs']";
        private const string TcidRowLinkSelector = "td a[href*='tcid.aspx']";
        private static readonly Random Randomizer = new();

        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public TargetChildIDTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(1)]
        public void NavigateToTargetChildInformationForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void NewTcidButtonDisplaysInfoAlert(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);

            OpenNewTcidForm(driver);

            var infoAlert = driver.WaitforElementToBeInDOM(By.CssSelector(InfoAlertSelector), 10)
                ?? throw new InvalidOperationException("Info alert did not appear after opening New TCID form.");

            var alertText = infoAlert.Text?.Trim() ?? string.Empty;
            _output.WriteLine($"[INFO] Info alert text: {alertText}");
            Assert.Contains("Complete this form upon the birth of the target child", alertText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("target child is the newborn", alertText, StringComparison.OrdinalIgnoreCase);

            var validationSummary = driver.FindElements(By.CssSelector("div[id$='ValidationSummary1'], div.validation-summary"))
                .FirstOrDefault();
            Assert.NotNull(validationSummary);
            _output.WriteLine("[PASS] Validation summary container located on the New TCID form.");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(3)]
        public void ValidationAndSubmission(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenNewTcidForm(driver);

            const string targetChildTab = "#TargetChild";
            const string miechvTab = "#MIECHV";
            const string additionalItemsTab = "#OptionalItems";
            var targetChildFullName = "Gwen";
            var targetChildLastName = "Venom";

            SwitchToTab(driver, miechvTab, "MIECHV");
            var summaryText = SubmitForm(driver);
            Assert.Contains("Term of Birth is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, CountOccurrences(summaryText, "First Name is required!"));
            Assert.Contains("Parity is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Initial validation errors displayed.");

            SwitchToTab(driver, targetChildTab, "Target Child");
            SelectRandomDropdownOption(driver, BirthTermDropdownSelector, "Target child birth term");
            SwitchToTab(driver, miechvTab, "MIECHV");
            summaryText = SubmitForm(driver);
            Assert.DoesNotContain("Term of Birth is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, CountOccurrences(summaryText, "First Name is required!"));
            Assert.Contains("Parity is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Term of Birth validation cleared after selection.");

            SwitchToTab(driver, targetChildTab, "Target Child");
            var firstNameInput = driver.WaitforElementToBeInDOM(By.CssSelector(FirstNameInputSelector), 10)
                ?? throw new InvalidOperationException("Target child first name input was not found.");
            WebElementHelper.SetInputValue(driver, firstNameInput, targetChildFullName, "Target child first name", triggerBlur: true);
            SwitchToTab(driver, miechvTab, "MIECHV");
            summaryText = SubmitForm(driver);
            Assert.Equal(1, CountOccurrences(summaryText, "First Name is required!"));
            Assert.Contains("Parity is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] One First Name validation message cleared after entering first name.");

            SwitchToTab(driver, targetChildTab, "Target Child");
            var lastNameInput = driver.WaitforElementToBeInDOM(By.CssSelector(LastNameInputSelector), 10)
                ?? throw new InvalidOperationException("Target child last name input was not found.");
            WebElementHelper.SetInputValue(driver, lastNameInput, targetChildLastName, "Target child last name", triggerBlur: true);
            SwitchToTab(driver, miechvTab, "MIECHV");
            summaryText = SubmitForm(driver);
            Assert.Equal(0, CountOccurrences(summaryText, "First Name is required!"));
            Assert.Contains("Parity is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] All name validation messages cleared after entering last name.");

            SwitchToTab(driver, additionalItemsTab, "Additional Items");
            SelectRandomDropdownOption(driver, ParityDropdownSelector, "Parity dropdown");
            SwitchToTab(driver, miechvTab, "MIECHV");
            summaryText = SubmitForm(driver);
            Assert.DoesNotContain("Parity is required.", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Gestational Age is required and must be a valid whole number!", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Parity selection accepted; Gestational Age validation displayed.");

            SwitchToTab(driver, targetChildTab, "Target Child");
            var gestationalInput = driver.WaitforElementToBeInDOM(By.CssSelector(GestationalAgeInputSelector), 10)
                ?? throw new InvalidOperationException("Gestational age input was not found.");
            WebElementHelper.SetInputValue(driver, gestationalInput, "-1", "Gestational age", triggerBlur: true);
            SwitchToTab(driver, miechvTab, "MIECHV");
            summaryText = SubmitForm(driver);
            Assert.Contains("Gestation Age must be between 0 and 40 weeks!", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Invalid gestational age triggered range validation.");

            SwitchToTab(driver, targetChildTab, "Target Child");
            WebElementHelper.SetInputValue(driver, gestationalInput, "38", "Gestational age", triggerBlur: true);
            _output.WriteLine("[PASS] Set gestational age within valid range without performing a final submit.");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(4)]
        public void ExistingTcidPrenatalCareValidation(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenExistingTcidEntry(driver);

            const string targetChildTab = "#TargetChild";
            const string miechvTab = "#MIECHV";

            SwitchToTab(driver, targetChildTab, "Target Child");
            SetPrenatalCareDate(driver, "12/01/25");

            SwitchToTab(driver, miechvTab, "MIECHV");
            var summaryText = SubmitForm(driver);
            Assert.Contains("Date Began Receiving Prenatal Care", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("can not start after the birth of the child", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Prenatal care date validation displayed for future date.");

            SwitchToTab(driver, targetChildTab, "Target Child");
            SetPrenatalCareDate(driver, "11/01/25");

            SwitchToTab(driver, miechvTab, "MIECHV");
            SubmitForm(driver, expectValidation: false);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving existing TCID.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Target Child Identification", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Existing TCID saved successfully after correcting prenatal care date.");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(5)]
        public void ExistingTcidBirthWeightValidation(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenExistingTcidEntry(driver);

            const string targetChildTab = "#TargetChild";
            const string miechvTab = "#MIECHV";

            ValidateBirthWeight(driver, targetChildTab, miechvTab, "-1", "-2");
            ValidateBirthWeight(driver, targetChildTab, miechvTab, "20", "20");
            ValidateBirthWeight(driver, targetChildTab, miechvTab, "17", "16");

            var validLbs = Randomizer.Next(0, 17);
            var validOz = Randomizer.Next(0, 16);

            SwitchToTab(driver, targetChildTab, "Target Child");
            SetBirthWeight(driver, validLbs.ToString(), validOz.ToString());

            SwitchToTab(driver, miechvTab, "MIECHV");
            SubmitForm(driver, expectValidation: false);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving birth weight updates.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Target Child Identification", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Existing TCID saved successfully after entering valid birth weight.");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(6)]
        public void HealthInsuranceOptionsRespectMedicaidSelection(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToTargetChildPage(driver, formsPane, pc1Id);
            OpenExistingTcidEntry(driver);

            const string healthInsuranceTab = "#HealthInsurance";
            SwitchToTab(driver, healthInsuranceTab, "Health Insurance");

            var medicaidDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(MedicaidDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medicaid dropdown was not found on the Health Insurance tab.");

            _output.WriteLine("[INFO] Selecting 'Yes' for Medicaid.");
            WebElementHelper.SelectDropdownOption(driver, medicaidDropdown, "Medicaid dropdown", "Yes", "1");
            EnsureMedicaidCaseNumberInputVisible(driver);
            var insuranceCheckboxes = GetHealthInsuranceCheckboxes(driver);
            foreach (var checkbox in insuranceCheckboxes)
            {
                Assert.False(checkbox.Enabled, $"Checkbox '{checkbox.GetAttribute("id")}' should be disabled when Medicaid is 'Yes'.");
            }
            _output.WriteLine("[PASS] Health insurance options were disabled when Medicaid was set to Yes.");

            _output.WriteLine("[INFO] Selecting 'No' for Medicaid.");
            WebElementHelper.SelectDropdownOption(driver, medicaidDropdown, "Medicaid dropdown", "No", "0");
            insuranceCheckboxes = GetHealthInsuranceCheckboxes(driver);
            foreach (var checkbox in insuranceCheckboxes)
            {
                Assert.True(checkbox.Enabled, $"Checkbox '{checkbox.GetAttribute("id")}' should be editable when Medicaid is 'No'.");
            }

            var otherCheckbox = GetHealthInsuranceOtherCheckbox(driver);
            var otherSpecifyInput = GetHealthInsuranceOtherSpecifyInput(driver);

            if (!otherCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, otherCheckbox);
                driver.WaitForReady(2);
                Thread.Sleep(300);
            }

            Assert.True(IsElementVisible(otherSpecifyInput), "Health insurance 'Other specify' field should be visible when 'Other' is selected and Medicaid is 'No'.");
            _output.WriteLine("[PASS] 'Other specify' text box displayed after selecting Other with Medicaid = No.");

            if (otherCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, otherCheckbox);
                driver.WaitForReady(2);
                Thread.Sleep(300);
            }

            _output.WriteLine("[INFO] Selecting 'Unknown' for Medicaid.");
            WebElementHelper.SelectDropdownOption(driver, medicaidDropdown, "Medicaid dropdown", "Unknown", "9");
            insuranceCheckboxes = GetHealthInsuranceCheckboxes(driver);
            foreach (var checkbox in insuranceCheckboxes)
            {
                Assert.True(checkbox.Enabled, $"Checkbox '{checkbox.GetAttribute("id")}' should be editable when Medicaid is 'Unknown'.");
            }

            otherCheckbox = GetHealthInsuranceOtherCheckbox(driver);
            otherSpecifyInput = GetHealthInsuranceOtherSpecifyInput(driver);

            if (!otherCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, otherCheckbox);
                driver.WaitForReady(2);
                Thread.Sleep(300);
            }

            Assert.True(IsElementVisible(otherSpecifyInput), "Health insurance 'Other specify' field should be visible when 'Other' is selected and Medicaid is 'Unknown'.");
            _output.WriteLine("[PASS] 'Other specify' text box displayed after selecting Other with Medicaid = Unknown.");

            medicaidDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(MedicaidDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medicaid dropdown was not found after finishing Unknown validation.");
            _output.WriteLine("[INFO] Selecting 'Yes' for Medicaid to capture case number.");
            WebElementHelper.SelectDropdownOption(driver, medicaidDropdown, "Medicaid dropdown", "Yes", "1");
            var medicaidCaseNumberInput = EnsureMedicaidCaseNumberInputVisible(driver);
            WebElementHelper.SetInputValue(driver, medicaidCaseNumberInput, "MCN12345", "Medicaid case number", triggerBlur: true);

            var hasMedicalProviderDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(HasMedicalProviderDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical provider question dropdown was not found on the Health Insurance tab.");

            _output.WriteLine("[INFO] Selecting 'No' for medical provider question (Q19).");
            WebElementHelper.SelectDropdownOption(driver, hasMedicalProviderDropdown, "Medical provider availability dropdown", "No", "0");
            AssertMedicalProviderDropdownsEnabledState(driver, shouldBeEnabled: false);
            var medicalProviderNotInListLink = GetMedicalProviderNotInListLink(driver);
            Assert.True(HasDisabledClass(medicalProviderNotInListLink), "'Not in List' doctor link should be disabled when Q19 is 'No'.");
            var medicalFacilityNotInListLink = GetMedicalFacilityNotInListLink(driver);
            Assert.True(HasDisabledClass(medicalFacilityNotInListLink), "'Not in List' facility link should be disabled when Q19 is 'No'.");
            _output.WriteLine("[PASS] Question 20 dropdowns were disabled when Has Medical Provider = No.");

            hasMedicalProviderDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(HasMedicalProviderDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical provider question dropdown was not found after updating the selection.");

            _output.WriteLine("[INFO] Selecting 'Yes' for medical provider question (Q19).");
            WebElementHelper.SelectDropdownOption(driver, hasMedicalProviderDropdown, "Medical provider availability dropdown", "Yes", "1");
            AssertMedicalProviderDropdownsEnabledState(driver, shouldBeEnabled: true);
            medicalProviderNotInListLink = GetMedicalProviderNotInListLink(driver);
            Assert.False(HasDisabledClass(medicalProviderNotInListLink), "'Not in List' doctor link should be enabled when Q19 is 'Yes'.");
            medicalFacilityNotInListLink = GetMedicalFacilityNotInListLink(driver);
            Assert.False(HasDisabledClass(medicalFacilityNotInListLink), "'Not in List' facility link should be enabled when Q19 is 'Yes'.");

            _output.WriteLine("[INFO] Opening the Medical Provider modal via 'Not in List'.");
            CommonTestHelper.ClickElement(driver, medicalProviderNotInListLink);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            var modal = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalProviderModalSelector), 10)
                ?? throw new InvalidOperationException("Medical provider modal did not appear after clicking 'Not in List'.");

            var modalFirstNameInput = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalFirstNameInputSelector, "Medical provider first name input", 5);
            var modalLastNameInput = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalLastNameInputSelector, "Medical provider last name input", 5);
            modalFirstNameInput.Clear();
            modalLastNameInput.Clear();

            var modalSubmitButton = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalSubmitButtonSelector, "Medical provider modal submit button", 5);
            CommonTestHelper.ClickElement(driver, modalSubmitButton);
            driver.WaitForReady(5);
            Thread.Sleep(300);

            var modalValidation = driver.FindElements(By.CssSelector(MedicalProviderModalValidationSelector))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                ?? throw new InvalidOperationException("Medical provider validation summary did not appear after submitting without a last name.");
            Assert.Contains("Provider's Last Name", modalValidation.Text, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Medical provider modal displayed validation when last name was missing.");

            var modalAddressInput = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalAddressInputSelector, "Medical provider address input", 5);
            var modalStateInput = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalStateInputSelector, "Medical provider state input", 5);
            var modalZipInput = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalZipInputSelector, "Medical provider zip input", 5);
            var modalPhoneInput = WebElementHelper.FindElementInModalOrPage(driver, MedicalProviderModalPhoneInputSelector, "Medical provider phone input", 5);

            WebElementHelper.SetInputValue(driver, modalFirstNameInput, "testone", "Medical provider first name input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, modalLastNameInput, "testtwo", "Medical provider last name input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, modalAddressInput, "aaaaaaaa", "Medical provider address input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, modalStateInput, "aa", "Medical provider state input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, modalZipInput, "00000", "Medical provider zip input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, modalPhoneInput, "000000000", "Medical provider phone input", triggerBlur: true);

            CommonTestHelper.ClickElement(driver, modalSubmitButton);
            driver.WaitForUpdatePanel(20);
            driver.WaitForReady(20);
            Thread.Sleep(1000);
            WaitForElementToDisappear(driver, MedicalProviderModalSelector, 10);
            SwitchToTab(driver, healthInsuranceTab, "Health Insurance");

            var providerDropdownAfterSave = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalProviderDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical provider dropdown was not found after saving the new provider.");
            var providerSelectAfterSave = new SelectElement(providerDropdownAfterSave);
            var matchingProviderOptions = providerSelectAfterSave.Options
                .Where(opt => opt.Text.Contains("testtwo", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(matchingProviderOptions.Any(), "Expected at least one provider option containing 'testtwo' after save.");
            var selectedOptionValue = providerSelectAfterSave.SelectedOption.GetAttribute("value");
            Assert.Contains(matchingProviderOptions, opt => string.Equals(opt.GetAttribute("value"), selectedOptionValue, StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"[PASS] Newly added provider '{providerSelectAfterSave.SelectedOption.Text}' appeared in Question 20 and was auto-selected.");

            medicalFacilityNotInListLink = GetMedicalFacilityNotInListLink(driver);
            _output.WriteLine("[INFO] Opening the Medical Facility modal via 'Not in List'.");
            CommonTestHelper.ClickElement(driver, medicalFacilityNotInListLink);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            var facilityModal = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalFacilityModalSelector), 10)
                ?? throw new InvalidOperationException("Medical facility modal did not appear after clicking 'Not in List'.");
            var facilitySubmitButton = WebElementHelper.FindElementInModalOrPage(driver, MedicalFacilityModalSubmitButtonSelector, "Medical facility modal submit button", 5);
            CommonTestHelper.ClickElement(driver, facilitySubmitButton);
            driver.WaitForReady(5);
            Thread.Sleep(300);

            var facilityValidation = driver.FindElements(By.CssSelector(MedicalFacilityModalValidationSelector))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                ?? throw new InvalidOperationException("Medical facility validation summary did not appear after submitting without data.");
            Assert.Contains("Facility", facilityValidation.Text, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Medical facility modal displayed validation when facility name was missing.");

            var facilityNameInput = FindInputInContainerByIdParts(facilityModal, "Medical facility name input", "MedicalFacility", "Name");
            var facilityAddressInput = FindInputInContainerByIdParts(facilityModal, "Medical facility address input", "MedicalFacility", "Address");
            var facilityCityInput = FindInputInContainerByIdParts(facilityModal, "Medical facility city input", "MedicalFacility", "City");
            var facilityStateInput = FindInputInContainerByIdParts(facilityModal, "Medical facility state input", "MedicalFacility", "State");
            var facilityZipInput = FindInputInContainerByIdParts(facilityModal, "Medical facility zip input", "MedicalFacility", "Zip");
            var facilityPhoneInput = FindInputInContainerByIdParts(facilityModal, "Medical facility phone input", "MedicalFacility", "Phone");

            WebElementHelper.SetInputValue(driver, facilityNameInput, "avengers", "Medical facility name input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, facilityAddressInput, "aaaa", "Medical facility address input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, facilityCityInput, "aaaaaa", "Medical facility city input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, facilityStateInput, "aa", "Medical facility state input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, facilityZipInput, "00000", "Medical facility zip input", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, facilityPhoneInput, "3434343434", "Medical facility phone input", triggerBlur: true);

            var facilitySubmitButtonAfterInputs = WebElementHelper.FindElementInModalOrPage(driver, MedicalFacilityModalSubmitButtonSelector, "Medical facility modal submit button", 5);
            CommonTestHelper.ClickElement(driver, facilitySubmitButtonAfterInputs);
            driver.WaitForUpdatePanel(20);
            driver.WaitForReady(20);
            Thread.Sleep(1000);
            WaitForElementToDisappear(driver, MedicalFacilityModalSelector, 10);
            SwitchToTab(driver, healthInsuranceTab, "Health Insurance");

            var facilityDropdownAfterSave = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalFacilityDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical facility dropdown was not found after saving the new facility.");
            var facilitySelectAfterSave = new SelectElement(facilityDropdownAfterSave);
            var matchingFacilityOptions = facilitySelectAfterSave.Options
                .Where(opt => opt.Text.Contains("avengers", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(matchingFacilityOptions.Any(), "Expected at least one facility option containing 'avengers' after save.");
            var selectedFacilityValue = facilitySelectAfterSave.SelectedOption.GetAttribute("value");
            Assert.Contains(matchingFacilityOptions, opt => string.Equals(opt.GetAttribute("value"), selectedFacilityValue, StringComparison.OrdinalIgnoreCase));
            _output.WriteLine($"[PASS] Newly added facility '{facilitySelectAfterSave.SelectedOption.Text}' appeared in Question 20 and was auto-selected.");

            _output.WriteLine("[INFO] Submitting TCID form to persist health insurance updates.");
            SubmitForm(driver, expectValidation: false);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving the TCID form.");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Target Child Identification", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] TCID form saved successfully with message: {toastMessage}");
        }

        private void NavigateToTargetChildPage(IPookieWebDriver driver, IWebElement formsPane, string pc1Id)
        {
            var targetChildLink = formsPane.FindElements(By.CssSelector(TargetChildLinkSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Target Child Information and Birth Outcomes link was not found in the Forms tab.");

            _output.WriteLine($"[INFO] Clicking link: {targetChildLink.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, targetChildLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url ?? string.Empty;
            _output.WriteLine($"[INFO] Current URL: {currentUrl}");

            Assert.Contains("TCIDs.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"pc1id={pc1Id}", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Target Child Information page opened with expected PC1 query string.");
        }

        private void OpenNewTcidForm(IPookieWebDriver driver)
        {
            var newTcidButton = driver.FindElements(By.CssSelector(NewTcidButtonSelector))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("New TCID", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("New TCID button was not found on the Target Child Information page.");

            _output.WriteLine("[INFO] Clicking New TCID button.");
            CommonTestHelper.ClickElement(driver, newTcidButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
        }

        private void SwitchToTab(IPookieWebDriver driver, string tabHref, string tabTitle)
        {
            var tabLink = driver.FindElements(By.CssSelector(
                    $"ul.nav.nav-pills li a[data-toggle='tab'][href='{tabHref}'], " +
                    $"ul.nav.nav-pills li a[title*='{tabTitle}']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Tab link '{tabTitle}' was not found on the TCID form.");

            _output.WriteLine($"[INFO] Switching to tab: {tabTitle}");
            CommonTestHelper.ClickElement(driver, tabLink);
            driver.WaitForReady(5);
            Thread.Sleep(300);

            var parentLi = tabLink.FindElements(By.XPath("ancestor::li[1]")).FirstOrDefault();
            if (parentLi != null)
            {
                Assert.Contains("active", parentLi.GetAttribute("class") ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }
        }

        private string SubmitForm(IPookieWebDriver driver, bool expectValidation = true)
        {
            return SubmitFormCore(driver, expectValidation, switchBackToAdditionalItems: false);
        }

        protected string SubmitFormFromAdditionalItemsTab(IPookieWebDriver driver, bool expectValidation = true)
        {
            return SubmitFormCore(driver, expectValidation, switchBackToAdditionalItems: true);
        }

        private string SubmitFormCore(IPookieWebDriver driver, bool expectValidation, bool switchBackToAdditionalItems)
        {
            var submitButton = driver.FindElements(By.CssSelector(SubmitButtonSelector))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found on the TCID form.");

            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            if (switchBackToAdditionalItems)
            {
                TrySwitchBackToAdditionalItems(driver);
            }

            if (!expectValidation)
            {
                return string.Empty;
            }

            var validationSummary = driver.FindElements(By.CssSelector(ValidationSummarySelector))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                ?? throw new InvalidOperationException("Validation summary errors were not displayed after submitting the TCID form.");

            var summaryText = validationSummary.Text?.Trim() ?? string.Empty;
            _output.WriteLine($"[INFO] Validation summary: {summaryText}");
            return summaryText;
        }

        private void SelectRandomDropdownOption(IPookieWebDriver driver, string selector, string description)
        {
            var dropdown = WebElementHelper.FindElementInModalOrPage(driver, selector, description, 10);
            var select = new SelectElement(dropdown);
            var validOptions = select.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")))
                .ToList();

            if (!validOptions.Any())
            {
                throw new InvalidOperationException($"No selectable options were found for {description}.");
            }

            var randomOption = validOptions[Randomizer.Next(validOptions.Count)];
            var optionText = randomOption.Text?.Trim() ?? randomOption.GetAttribute("value") ?? string.Empty;
            var optionValue = randomOption.GetAttribute("value");
            WebElementHelper.SelectDropdownOption(driver, dropdown, description, optionText, optionValue);
            _output.WriteLine($"[INFO] Selected {description}: {optionText} ({optionValue})");
        }

        private void OpenExistingTcidEntry(IPookieWebDriver driver)
        {
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector(TcidGridSelector), 20)
                ?? throw new InvalidOperationException("TCID grid was not found on the Target Child Information page.");

            var entryLink = grid.FindElements(By.CssSelector(TcidRowLinkSelector))
                .FirstOrDefault(link => link.Displayed && !string.IsNullOrWhiteSpace(link.Text))
                ?? throw new InvalidOperationException("No TCID link was available to open.");

            var linkText = entryLink.Text?.Trim() ?? "(unnamed)";
            _output.WriteLine($"[INFO] Opening existing TCID entry: {linkText}");

            CommonTestHelper.ClickElement(driver, entryLink);
            driver.WaitForReady(30);
            driver.WaitForUpdatePanel(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url ?? string.Empty;
            Assert.Contains("tcid.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tcpk=", currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Navigated to existing TCID form: {currentUrl}");
        }

        private void SetPrenatalCareDate(IPookieWebDriver driver, string dateValue)
        {
            var prenatalInput = driver.FindElements(By.CssSelector(PrenatalCareInputSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Prenatal care date input was not found on the TCID form.");

            WebElementHelper.SetInputValue(driver, prenatalInput, dateValue, "First Prenatal Care Visit date", triggerBlur: true);
            _output.WriteLine($"[INFO] Set prenatal care date to {dateValue}");
        }

        private void ValidateBirthWeight(IPookieWebDriver driver, string targetTab, string miechvTab, string pounds, string ounces)
        {
            SwitchToTab(driver, targetTab, "Target Child");
            SetBirthWeight(driver, pounds, ounces);

            SwitchToTab(driver, miechvTab, "MIECHV");
            var summaryText = SubmitForm(driver);

            Assert.Contains("Birth weight pounds must be less than 17", summaryText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Birth weight ounces must be less than 16", summaryText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Birth weight validation displayed for values lbs={pounds}, oz={ounces}");
        }

        private void SetBirthWeight(IPookieWebDriver driver, string pounds, string ounces)
        {
            var lbsInput = driver.FindElements(By.CssSelector(BirthWeightLbsInputSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Birth weight pounds input was not found.");

            var ozInput = driver.FindElements(By.CssSelector(BirthWeightOzInputSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Birth weight ounces input was not found.");

            WebElementHelper.SetInputValue(driver, lbsInput, pounds, "Birth weight pounds", triggerBlur: true);
            WebElementHelper.SetInputValue(driver, ozInput, ounces, "Birth weight ounces", triggerBlur: true);
            _output.WriteLine($"[INFO] Set birth weight to {pounds} lbs {ounces} oz");
        }

        private static int CountOccurrences(string text, string value)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return Regex.Matches(text, Regex.Escape(value), RegexOptions.IgnoreCase).Count;
        }

        private static IReadOnlyList<IWebElement> GetHealthInsuranceCheckboxes(IPookieWebDriver driver)
        {
            var checkboxes = driver.FindElements(By.CssSelector(HealthInsuranceCheckboxesSelector))
                .Where(el => el.Displayed)
                .ToList();

            if (!checkboxes.Any())
            {
                throw new InvalidOperationException("Health insurance checkboxes were not found on the TCID form.");
            }

            return checkboxes;
        }

        private static IWebElement GetHealthInsuranceOtherCheckbox(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector(HealthInsuranceOtherCheckboxSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Health insurance 'Other' checkbox was not found on the TCID form.");
        }

        private static IWebElement GetHealthInsuranceOtherSpecifyInput(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector(HealthInsuranceOtherSpecifyInputSelector))
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Health insurance 'Other specify' input was not found on the TCID form.");
        }

        private static bool IsElementVisible(IWebElement element)
        {
            if (element == null)
            {
                return false;
            }

            try
            {
                return element.Displayed && !string.Equals(element.GetCssValue("display"), "none", StringComparison.OrdinalIgnoreCase);
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        }

        private void AssertMedicalProviderDropdownsEnabledState(IPookieWebDriver driver, bool shouldBeEnabled)
        {
            var providerDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalProviderDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical provider dropdown was not found on the Health Insurance tab.");
            var facilityDropdown = driver.WaitforElementToBeInDOM(By.CssSelector(MedicalFacilityDropdownSelector), 10)
                ?? throw new InvalidOperationException("Medical facility dropdown was not found on the Health Insurance tab.");

            AssertDropdownEnabledState(providerDropdown, shouldBeEnabled, "Medical provider dropdown");
            AssertDropdownEnabledState(facilityDropdown, shouldBeEnabled, "Medical facility dropdown");
        }

        private static void AssertDropdownEnabledState(IWebElement dropdown, bool shouldBeEnabled, string description)
        {
            var disabledAttr = dropdown.GetAttribute("disabled");
            if (shouldBeEnabled)
            {
                Assert.True(dropdown.Enabled && string.IsNullOrWhiteSpace(disabledAttr), $"{description} should be enabled.");
            }
            else
            {
                var isDisabled = !dropdown.Enabled ||
                                 string.Equals(disabledAttr, "true", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(disabledAttr, "disabled", StringComparison.OrdinalIgnoreCase);
                Assert.True(isDisabled, $"{description} should be disabled.");
            }
        }

        private static IWebElement GetMedicalProviderNotInListLink(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector(MedicalProviderNotInListLinkSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("'Not in List' medical provider link was not found on the Health Insurance tab.");
        }

        private static bool HasDisabledClass(IWebElement element)
        {
            var classAttr = element.GetAttribute("class") ?? string.Empty;
            return classAttr
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(c => c.Equals("disabled", StringComparison.OrdinalIgnoreCase));
        }

        private static void WaitForElementToDisappear(IPookieWebDriver driver, string cssSelector, int timeoutSeconds = 10)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= endTime)
            {
                var visibleElement = driver.FindElements(By.CssSelector(cssSelector))
                    .FirstOrDefault(el => el.Displayed);
                if (visibleElement == null)
                {
                    return;
                }

                Thread.Sleep(200);
            }

            throw new TimeoutException($"Element '{cssSelector}' did not disappear within {timeoutSeconds} seconds.");
        }

        private static IWebElement GetMedicalFacilityNotInListLink(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector(MedicalFacilityNotInListLinkSelector))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("'Not in List' medical facility link was not found on the Health Insurance tab.");
        }

        private IWebElement EnsureMedicaidCaseNumberInputVisible(IPookieWebDriver driver)
        {
            var medicaidCaseInput = driver.FindElements(By.CssSelector(MedicaidCaseNumberInputSelector))
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Medicaid case number input was not found on the Health Insurance tab.");

            Assert.True(IsElementVisible(medicaidCaseInput), "Medicaid case number input should be visible when Medicaid is 'Yes'.");
            return medicaidCaseInput;
        }

        private static IWebElement FindInputInContainerByIdParts(IWebElement container, string description, params string[] idParts)
        {
            var inputs = container.FindElements(By.CssSelector("input"));
            foreach (var input in inputs)
            {
                var inputId = input.GetAttribute("id") ?? string.Empty;
                if (idParts.All(part => inputId.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return input;
                }
            }

            throw new InvalidOperationException($"Unable to locate {description}.");
        }

        private static void SelectDropdownPlaceholderOption(IWebElement dropdown, string description)
        {
            var selectElement = new SelectElement(dropdown);
            var placeholderOption = selectElement.Options.FirstOrDefault(opt => string.IsNullOrWhiteSpace(opt.GetAttribute("value")));
            if (placeholderOption == null)
            {
                throw new InvalidOperationException($"No placeholder option was available for {description}.");
            }

            try
            {
                selectElement.SelectByValue(string.Empty);
            }
            catch (NoSuchElementException)
            {
                placeholderOption.Click();
            }
        }

        private void TrySwitchBackToAdditionalItems(IPookieWebDriver driver)
        {
            try
            {
                var tabLink = driver.FindElements(By.CssSelector(
                        $"ul.nav.nav-pills li a[data-toggle='tab'][href='{AdditionalItemsTabSelector}'], " +
                        $"ul.nav.nav-pills li a[title*='Additional Items']"))
                    .FirstOrDefault(el => el.Displayed);

                if (tabLink != null)
                {
                    CommonTestHelper.ClickElement(driver, tabLink);
                    driver.WaitForReady(5);
                    Thread.Sleep(300);
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WARN] Unable to switch back to Additional Items tab automatically: {ex.Message}");
            }
        }

        protected void NavigateBackToExistingTcid(IPookieWebDriver driver, string pc1Id)
        {
            driver.Navigate().GoToUrl($"{_config.AppUrl}/Pages/TCIDs.aspx?pc1id={pc1Id}");
            driver.WaitForReady(15);
            OpenExistingTcidEntry(driver);
        }
    }
}

