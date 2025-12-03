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
    public class TargetChildIDTests : IClassFixture<AppConfig>
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
            var submitButton = driver.FindElements(By.CssSelector(SubmitButtonSelector))
                .FirstOrDefault(el => el.Displayed && el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Submit button was not found on the TCID form.");

            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

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
    }
}

