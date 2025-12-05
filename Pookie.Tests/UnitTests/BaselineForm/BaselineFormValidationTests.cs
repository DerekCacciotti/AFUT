using System;
using System.Collections.Generic;
using System.Globalization;
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
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
        public class BaselineFormValidationTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;
            private static readonly Random RandomGenerator = new();
            private static readonly object RandomLock = new();

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public BaselineFormValidationTests(AppConfig config, ITestOutputHelper output)
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
        public void SubmitShowsRelationshipValidationMessage(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToBaselineForm(driver, formsPane);
            _output.WriteLine("[PASS] Intake (Baseline) form loaded successfully");

            SelectRelationshipDropdown(driver);
            _output.WriteLine("[INFO] Ensured relationship dropdown is set to '--Select--'");

            var submitButton = driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Submit", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    (el.GetAttribute("title")?.Contains("Save", StringComparison.OrdinalIgnoreCase) ?? true))
                ?? throw new InvalidOperationException("Baseline form Submit button was not found.");

            _output.WriteLine($"[INFO] Found Submit button: {submitButton.Text?.Trim()}");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Submit button without entering required fields");

            var validationElement = driver.FindElements(By.CssSelector(
                    ".text-danger, " +
                    "span.text-danger, " +
                    "span[style*='color: red'], " +
                    "span[style*='color:Red'], " +
                    "div.alert.alert-danger"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    !string.IsNullOrWhiteSpace(el.Text) &&
                    el.Text.Contains("relationship to target child", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(validationElement);
            var validationText = validationElement!.Text.Trim();
            _output.WriteLine($"[INFO] Validation message displayed: {validationText}");
            Assert.Contains("Please enter relationship to target child", validationText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Relationship validation is shown when submitting empty Baseline form");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void ConditionalQuestionsRespondToBaselineSelections(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Navigated to Forms tab for conditional question test");

            NavigateToBaselineForm(driver, formsPane);
            _output.WriteLine("[PASS] Intake (Baseline) form loaded successfully");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlGender']",
                "Gender dropdown",
                "Female",
                "02");
            _output.WriteLine("[INFO] Selected gender");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlRelation']",
                "Relationship to target child dropdown",
                "2. Step-parent",
                "02");
            _output.WriteLine("[INFO] Selected relationship to target child");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlMaritalStatus']",
                "Marital status dropdown",
                "2. Never Married",
                "02");
            _output.WriteLine("[INFO] Selected marital status");

            var blackRaceCheckbox = driver.FindElements(By.CssSelector("input[type='checkbox'][id*='chkRace_Black']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Race checkbox was not found.");

            if (!blackRaceCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, blackRaceCheckbox);
                driver.WaitForReady(2);
            }
            _output.WriteLine("[INFO] Selected 'Black or African American' race");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlHispanic']",
                "Ethnicity dropdown",
                "Hispanic",
                "True");
            _output.WriteLine("[INFO] Selected ethnicity");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlBornUSA']",
                "Born in USA dropdown",
                "No",
                "0");
            var bornSection = driver.WaitforElementToBeInDOM(By.CssSelector("#divBornUSA_PC1Form"), 5)
                ?? throw new InvalidOperationException("Born in USA follow-up section was not found.");
            Assert.True(ElementIsDisplayed(bornSection), "Country of birth section should be visible when selecting No for Born in USA.");

            var birthCountryInput = bornSection.FindElements(By.CssSelector("input.form-control[id*='txtBirthCountry']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Birth country input was not found.");
            WebElementHelper.SetInputValue(driver, birthCountryInput, "Canada", "Birth country", triggerBlur: true);

            var yearsInUsaInput = bornSection.FindElements(By.CssSelector("input.form-control[id*='txtYearsInUSA']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Years in USA input was not found.");
            WebElementHelper.SetInputValue(driver, yearsInUsaInput, "5", "Years in USA", triggerBlur: true);
            _output.WriteLine("[INFO] Filled out Born in USA follow-up fields");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlBornUSA']",
                "Born in USA dropdown",
                "Yes",
                "1");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(300);

            var bornSectionHidden = WaitUntilChildrenHidden(
                driver,
                "#divBornUSA_PC1Form",
                ".row",
                8,
                _output,
                "Born in USA follow-up section");
            Assert.True(bornSectionHidden, "Country of birth follow-up rows should hide when selecting Yes for Born in USA.");
            _output.WriteLine("[PASS] Born in USA follow-up section hides after selecting Yes");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPrimaryLanguage']",
                "Primary language dropdown",
                "99. Other",
                "99");
            driver.WaitForReady(3);

            var specifyLanguageRow = driver.WaitforElementToBeInDOM(By.CssSelector("#divPrimaryLanguageSpecify_PC1Form"), 5)
                ?? throw new InvalidOperationException("Specify Primary Language row was not found.");
            Assert.True(ElementIsDisplayed(specifyLanguageRow), "Specify primary language row should appear when selecting Other.");

            var specifyLanguageInput = specifyLanguageRow.FindElements(By.CssSelector("input.form-control"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Specify primary language input was not found.");
            WebElementHelper.SetInputValue(driver, specifyLanguageInput, "Elvish", "Specify primary language", triggerBlur: true);
            _output.WriteLine("[INFO] Entered specify primary language text");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlPrimaryLanguage']",
                "Primary language dropdown",
                "1. English",
                "01");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(5);
            Thread.Sleep(300);

            var languageSpecifyHidden = WaitUntilElementHidden(
                driver,
                "#divPrimaryLanguageSpecify_PC1Form",
                8,
                _output,
                "Specify primary language row");
            Assert.True(languageSpecifyHidden, "Specify primary language row should hide when selecting a predefined language.");
            _output.WriteLine("[PASS] Specify primary language row hides after selecting a predefined language option");

            var (gradeText, gradeValue) = SelectRandomDropdownOption(
                driver,
                "select.form-control[id*='ddlHighestGrade']",
                "Highest grade completed dropdown");
            _output.WriteLine($"[INFO] Selected random highest grade option: {gradeText} (value: {gradeValue})");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlEducationalEnrollment']",
                "Educational enrollment dropdown",
                "Yes",
                "1");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);

            var enrollmentSection = driver.WaitforElementToBeInDOM(By.CssSelector("#divEducationalEnrollment_PC1Form"), 5)
                ?? throw new InvalidOperationException("Educational enrollment hours section was not found.");
            Assert.True(ElementIsDisplayed(enrollmentSection), "Educational enrollment section should display when selecting Yes.");
            _output.WriteLine("[INFO] Educational enrollment section displayed after selecting Yes");

            ClickSubmitButton(driver);
            _output.WriteLine("[INFO] Clicked submit to trigger hours per month validation");

            var hoursValidation = driver.FindElements(By.CssSelector(
                    ".text-danger, span.text-danger, span[style*='color: red'], span[style*='color:Red'], div.alert.alert-danger"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    !string.IsNullOrWhiteSpace(el.Text) &&
                    el.Text.Contains("Hours per month", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(hoursValidation);
            _output.WriteLine($"[PASS] Hours per month validation displayed: {hoursValidation!.Text.Trim()}");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlEducationalEnrollment']",
                "Educational enrollment dropdown",
                "No",
                "0");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);

            var enrollmentHidden = WaitUntilElementHidden(
                driver,
                "#divEducationalEnrollment_PC1Form",
                8,
                _output,
                "Educational enrollment hours section");
            Assert.True(enrollmentHidden, "Educational enrollment section should hide when selecting No.");
            _output.WriteLine("[PASS] Educational enrollment section hides after selecting No");

            var programCheckboxes = driver.FindElements(By.CssSelector(".ProgramType_PC1Form input[type='checkbox']")).ToList();
            Assert.NotEmpty(programCheckboxes);

            foreach (var checkbox in programCheckboxes)
            {
                Assert.False(checkbox.Enabled, "Program type checkbox should be disabled when enrollment is No.");
            }

            var programOtherCheckbox = programCheckboxes.FirstOrDefault(cb =>
                cb.GetAttribute("id")?.EndsWith("_9", StringComparison.OrdinalIgnoreCase) == true)
                ?? throw new InvalidOperationException("Program Type 'Other' checkbox was not found.");
            _output.WriteLine($"[INFO] Program type 'Other' checkbox disabled attribute: {programOtherCheckbox.GetAttribute("disabled")}");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlEducationalEnrollment']",
                "Educational enrollment dropdown",
                "Yes",
                "1");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);

            enrollmentSection = driver.WaitforElementToBeInDOM(By.CssSelector("#divEducationalEnrollment_PC1Form"), 5)
                ?? throw new InvalidOperationException("Educational enrollment hours section was not found after reselecting Yes.");
            Assert.True(ElementIsDisplayed(enrollmentSection), "Educational enrollment section should display after switching back to Yes.");

            var hoursInput = enrollmentSection.FindElements(By.CssSelector("input.form-control[id*='txtEduMonthlyHours']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Hours per month input was not found.");

            var invalidHours = GetRandomNumber(451, 600).ToString(CultureInfo.InvariantCulture);
            WebElementHelper.SetInputValue(driver, hoursInput, invalidHours, "Educational hours per month", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered invalid hours per month (>450): {invalidHours}");

            ClickSubmitButton(driver);
            var hoursRangeValidation = FindValidationMessage(
                driver,
                "Educational hours range validation",
                "Please Specify number of hours",
                "between 0 and 450");
            Assert.NotNull(hoursRangeValidation);
            _output.WriteLine($"[PASS] Educational hours range validation displayed: {hoursRangeValidation!.Text.Trim()}");

            hoursInput = driver.FindElements(By.CssSelector("input.form-control[id*='txtEduMonthlyHours']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Hours per month input was not found after range validation.");

            var validHours = GetRandomNumber(1, 450).ToString(CultureInfo.InvariantCulture);
            WebElementHelper.SetInputValue(driver, hoursInput, validHours, "Educational hours per month", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered valid hours per month (1-450): {validHours}");

            ClickSubmitButton(driver);
            var programValidation = FindValidationMessage(
                driver,
                "Program type validation",
                "PC1 You must specify a education or employment program");

            Assert.NotNull(programValidation);
            _output.WriteLine($"[PASS] Program type validation displayed: {programValidation!.Text.Trim()}");

            programCheckboxes = driver.FindElements(By.CssSelector(".ProgramType_PC1Form input[type='checkbox']")).ToList();
            Assert.True(programCheckboxes.Any(cb => cb.Enabled), "Program type checkboxes should be enabled when enrollment is Yes.");

            programOtherCheckbox = programCheckboxes.FirstOrDefault(cb =>
                cb.GetAttribute("id")?.EndsWith("_9", StringComparison.OrdinalIgnoreCase) == true)
                ?? throw new InvalidOperationException("Program Type 'Other' checkbox was not found.");

            Assert.True(programOtherCheckbox.Enabled, "Program type 'Other' checkbox should be enabled when enrollment is Yes.");
            if (!programOtherCheckbox.Selected)
            {
                CommonTestHelper.ClickElement(driver, programOtherCheckbox);
                driver.WaitForReady(2);
            }
            _output.WriteLine("[INFO] Selected 'Other' program type checkbox");

            var programSpecifyRow = driver.WaitforElementToBeInDOM(By.CssSelector("#divProgramTypeSpecify_PC1Form"), 5)
                ?? throw new InvalidOperationException("Specify Program row was not found after selecting Other.");
            Assert.True(ElementIsDisplayed(programSpecifyRow), "Specify Program row should display after selecting Other program type.");

            var programSpecifyInput = programSpecifyRow.FindElements(By.CssSelector("input.form-control"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Specify Program input was not found.");
            WebElementHelper.SetInputValue(driver, programSpecifyInput, $"Program {validHours}", "Specify Program", triggerBlur: true);
            _output.WriteLine("[PASS] Specify Program input displayed and populated after selecting Other");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlIsCurrenltyEmployed']",
                "Currently employed dropdown",
                "Yes",
                "1");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);

            var employmentStartInput = driver.FindElements(By.CssSelector("input.form-control[id*='txtEmploymentStartDate'], input[id*='txtEmploymentStartDate']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Employment start date input was not found.");
            Assert.True(employmentStartInput.Enabled, "Employment start date input should be enabled when currently employed is Yes.");

            var employmentHoursInput = driver.FindElements(By.CssSelector("input[id*='txtEmploymentMonthlyHours']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Employment monthly hours input was not found.");
            Assert.True(employmentHoursInput.Enabled, "Employment monthly hours input should be enabled when currently employed is Yes.");

            var employmentWagesInput = driver.FindElements(By.CssSelector("input[id*='txtEmploymentMonthlyWages']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Employment monthly wages input was not found.");
            Assert.True(employmentWagesInput.Enabled, "Employment monthly wages input should be enabled when currently employed is Yes.");
            _output.WriteLine("[INFO] Employment inputs are enabled for 'Yes' selection");

            ClickSubmitButton(driver);
            var employmentValidation = driver.FindElements(By.CssSelector(
                    ".text-danger, span.text-danger, span[style*='color: red'], span[style*='color:Red'], div.alert.alert-danger"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    !string.IsNullOrWhiteSpace(el.Text) &&
                    el.Text.Contains("employment start date", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(employmentValidation);
            _output.WriteLine($"[PASS] Employment validation displayed: {employmentValidation!.Text.Trim()}");

            WebElementHelper.SelectDropdownOption(
                driver,
                "select.form-control[id*='ddlIsCurrenltyEmployed']",
                "Currently employed dropdown",
                "No",
                "0");
            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);

            var previouslyEmployedDropdown = driver.WaitforElementToBeInDOM(By.CssSelector("select[id*='ddlPreviouslyEmployed']"), 5)
                ?? throw new InvalidOperationException("Previously employed dropdown was not found.");
            var lookedForEmploymentDropdown = driver.WaitforElementToBeInDOM(By.CssSelector("select[id*='ddlLooked4Employment']"), 5)
                ?? throw new InvalidOperationException("Looked for employment dropdown was not found.");

            Assert.True(previouslyEmployedDropdown.Enabled, "Previously employed dropdown should be enabled when currently employed is No.");
            Assert.True(lookedForEmploymentDropdown.Enabled, "Looked for employment dropdown should be enabled when currently employed is No.");
            _output.WriteLine("[INFO] Previously employed and looked for work dropdowns are enabled after selecting No");

            WebElementHelper.SelectDropdownOption(driver, previouslyEmployedDropdown, "Previously employed dropdown", "Yes", "1");
            WebElementHelper.SelectDropdownOption(driver, lookedForEmploymentDropdown, "Looked for employment dropdown", "No", "0");

            ClickSubmitButton(driver);
            var toastMessage = WebElementHelper.GetToastMessage(driver, 2000);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed after saving the Baseline form.");
            _output.WriteLine($"[INFO] Toast message: {toastMessage}");

            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("[PASS] Baseline form saved successfully with expected toast message");
        }

        private void NavigateToBaselineForm(IPookieWebDriver driver, IWebElement formsPane)
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

        private void SelectRelationshipDropdown(IPookieWebDriver driver)
        {
            var dropdownSelector = "select.form-control[id$='ddlRelation'], select[id*='PC1Form_ddlRelation']";
            WebElementHelper.SelectDropdownOption(
                driver,
                dropdownSelector,
                "Relationship to target child dropdown",
                "--Select--",
                string.Empty);
        }

        private static bool ElementIsDisplayed(IWebElement? element)
        {
            if (element == null)
            {
                return false;
            }

            try
            {
                return element.Displayed;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        }

        private static bool WaitUntilElementHidden(
            IPookieWebDriver driver,
            string cssSelector,
            int timeoutSeconds,
            ITestOutputHelper output,
            string description)
        {
            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            var iteration = 1;
            while (DateTime.Now <= end)
            {
                var elements = driver.FindElements(By.CssSelector(cssSelector)).ToList();

                if (!elements.Any())
                {
                    output?.WriteLine($"[INFO] {description}: no elements match '{cssSelector}' (iteration {iteration}). Assuming hidden.");
                    return true;
                }

                var states = elements.Select((el, index) => new
                    {
                        Index = index,
                        Displayed = ElementIsDisplayed(el),
                        Style = el.GetAttribute("style") ?? string.Empty,
                        Classes = el.GetAttribute("class") ?? string.Empty
                    })
                    .ToList();

                var anyDisplayed = states.Any(state => state.Displayed);
                foreach (var state in states)
                {
                    output?.WriteLine(
                        $"[DEBUG] {description} iteration {iteration}: element {state.Index} displayed={state.Displayed}, class='{state.Classes}', style='{state.Style}'");
                }

                if (!anyDisplayed)
                {
                    output?.WriteLine($"[INFO] {description}: all elements hidden after {iteration} iterations.");
                    return true;
                }

                iteration++;
                Thread.Sleep(200);
            }

            output?.WriteLine($"[WARN] {description}: elements still visible after {timeoutSeconds} seconds.");
            return false;
        }

        private static bool WaitUntilChildrenHidden(
            IPookieWebDriver driver,
            string parentSelector,
            string childSelector,
            int timeoutSeconds,
            ITestOutputHelper output,
            string description)
        {
            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            var iteration = 1;
            while (DateTime.Now <= end)
            {
                var parent = driver.FindElements(By.CssSelector(parentSelector)).FirstOrDefault();
                if (parent == null)
                {
                    output?.WriteLine($"[WARN] {description}: parent '{parentSelector}' not found (iteration {iteration}), assuming hidden.");
                    return true;
                }

                var children = parent.FindElements(By.CssSelector(childSelector)).ToList();
                if (!children.Any())
                {
                    output?.WriteLine($"[INFO] {description}: no children '{childSelector}' inside parent, assuming hidden.");
                    return true;
                }

                var states = children.Select((child, index) =>
                {
                    var style = child.GetAttribute("style") ?? string.Empty;
                    var classes = child.GetAttribute("class") ?? string.Empty;
                    var displayed = ElementIsDisplayed(child);
                    var hiddenByStyle = style.Contains("display: none", StringComparison.OrdinalIgnoreCase);
                    return new
                    {
                        Index = index,
                        Displayed = displayed,
                        HiddenByStyle = hiddenByStyle,
                        Style = style,
                        Classes = classes
                    };
                }).ToList();

                foreach (var state in states)
                {
                    output?.WriteLine($"[DEBUG] {description} iteration {iteration}: child {state.Index} displayed={state.Displayed}, hiddenByStyle={state.HiddenByStyle}, class='{state.Classes}', style='{state.Style}'");
                }

                if (states.All(state => !state.Displayed || state.HiddenByStyle))
                {
                    output?.WriteLine($"[INFO] {description}: all child rows hidden after {iteration} iterations.");
                    return true;
                }

                iteration++;
                Thread.Sleep(200);
            }

            output?.WriteLine($"[WARN] {description}: child rows still visible after {timeoutSeconds} seconds.");
            return false;
        }

        private static (string text, string value) SelectRandomDropdownOption(
            IPookieWebDriver driver,
            string cssSelector,
            string description)
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

            return (optionText, optionValue ?? string.Empty);
        }

        private static int GetRandomNumber(int minInclusive, int maxInclusive)
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

        private void ClickSubmitButton(IPookieWebDriver driver)
        {
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
        }

        private IWebElement FindSubmitButton(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Submit", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    (el.GetAttribute("title")?.Contains("Save", StringComparison.OrdinalIgnoreCase) ?? true))
                ?? throw new InvalidOperationException("Baseline form Submit button was not found.");
        }

        private IWebElement? FindValidationMessage(
            IPookieWebDriver driver,
            string description,
            params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                throw new ArgumentException("At least one keyword is required to locate a validation message.", nameof(keywords));
            }

            var candidates = driver.FindElements(By.CssSelector(
                    ".text-danger, span.text-danger, span[style*='color: red'], span[style*='color:Red'], div.alert.alert-danger, .validation-summary-errors li, .validation-summary"))
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
    }
}


