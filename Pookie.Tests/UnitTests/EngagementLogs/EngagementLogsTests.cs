using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.EngagementLogs
{
    public class EngagementLogsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;
        private const string TargetPc1Id = "EC01001408989";

        public EngagementLogsTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void CheckingTheEngagementLogButton()
        {
            using var driver = _driverFactory.CreateDriver();

            var steps = new List<(string Action, string Result)>();
            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToEngagementLog(driver, steps);
            var targetPc1Id = TargetPc1Id;

            var engagementSummary = driver.FindElements(By.CssSelector(".panel-body, .card-body, .form-group, .list-group"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .FirstOrDefault();

            string? pc1IdDisplay = driver.FindElements(By.CssSelector("[id$='lblPC1ID'], [id$='lblPc1Id'], .pc1-id, .pc1-id-value"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(pc1IdDisplay))
            {
                pc1IdDisplay = driver.FindElements(By.CssSelector(".panel-body, .card-body, .form-group, .list-group, .list-group-item, .row"))
                    .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text) && el.Text.Contains(targetPc1Id, StringComparison.OrdinalIgnoreCase))
                    .Select(el => el.Text.Trim())
                    .FirstOrDefault();
            }

            Assert.False(string.IsNullOrWhiteSpace(pc1IdDisplay), "Unable to locate PC1 ID on Engagement Log page.");
            Assert.Contains(targetPc1Id, pc1IdDisplay, StringComparison.OrdinalIgnoreCase);
            steps.Add(("PC1 verification", $"Engagement Log shows PC1 {targetPc1Id}"));

            if (!string.IsNullOrWhiteSpace(engagementSummary))
            {
                var clipped = engagementSummary.Length > 400 ? engagementSummary[..400] + "..." : engagementSummary;
                steps.Add(("Engagement Log content", clipped));
            }
            else
            {
                steps.Add(("Engagement Log content", "(no visible content)"));
            }

            foreach (var step in steps)
            {
                _output.WriteLine($"{step.Action}: {step.Result}");
            }
        }

        [Fact]
        public void CheckingValdiationOneMonthIsOver()
        {
            using var driver = _driverFactory.CreateDriver();

            var steps = new List<(string Action, string Result)>();
            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToEngagementLog(driver, steps);

            var caseStatusDropdown = OpenNewFormAndGetCaseStatusDropdown(driver, steps);
            var caseStatusSelect = new SelectElement(caseStatusDropdown);
            caseStatusSelect.SelectByValue("01");
            steps.Add(("Case status", "Selected option value 01"));

            var finalSubmitButton = FindElementInModalOrPage(
                driver,
                "div.panel-footer #Buttons a#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_Submit1_LoginView1_btnSubmit.btn.btn-primary," +
                " a#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_Submit1_LoginView1_btnSubmit.btn.btn-primary",
                "Final Submit button",
                15);
            ClickElement(driver, finalSubmitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            steps.Add(("Final submit", "Triggered engagement log validation"));

            var confirmationMessage = driver.FindElements(By.CssSelector(".modal-body, .modal-dialog .alert, .alert, .validation-summary-errors, .text-success, .text-danger, .panel-body"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .FirstOrDefault() ?? "(no confirmation message)";
            var confirmationSnippet = confirmationMessage.Length > 400 ? confirmationMessage[..400] + "..." : confirmationMessage;
            steps.Add(("Submit new form", confirmationSnippet));

            foreach (var step in steps)
            {
                _output.WriteLine($"{step.Action}: {step.Result}");
            }

            Assert.Contains("You can not enter a Engagement Log record with a case status of 1", confirmationSnippet, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CheckingAdditionalQuestionsAppearWhenCaseStatusTwoSelected()
        {
            using var driver = _driverFactory.CreateDriver();

            var steps = new List<(string Action, string Result)>();
            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToEngagementLog(driver, steps);

            var caseStatusDropdown = OpenNewFormAndGetCaseStatusDropdown(driver, steps);
            var caseStatusSelect = new SelectElement(caseStatusDropdown);
            caseStatusSelect.SelectByValue("02");
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            steps.Add(("Case status", "Selected option value 02"));

            var caseAssignedSection = driver.WaitforElementToBeInDOM(By.CssSelector("#trAssessmentCompleted"), 10)
                ?? throw new InvalidOperationException("Case assignment section was not found.");

            Assert.True(caseAssignedSection.Displayed, "Case assignment section was not displayed.");

            var yesRadio = driver.FindElement(By.CssSelector("input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_rbtnAssigned"));
            var noRadio = driver.FindElement(By.CssSelector("input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_rbtnNotAssigned"));
            var workerDropdown = driver.FindElement(By.CssSelector("select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlFSW"));
            var assignDateInput = driver.FindElement(By.CssSelector("input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtFSWDate"));

            Assert.True(yesRadio.Displayed && noRadio.Displayed, "Case assignment radio buttons were not visible.");
            Assert.True(workerDropdown.Displayed, "Worker selection dropdown was not visible.");
            Assert.True(assignDateInput.Displayed, "Worker assignment date input was not visible.");

            steps.Add(("Additional questions", "Case assignment section displayed with radio buttons, worker dropdown, and date input"));

            foreach (var step in steps)
            {
                _output.WriteLine($"{step.Action}: {step.Result}");
            }
        }

        [Fact]
        public void CheckingTerminationFieldsAppearWhenCaseStatusTwoAndCaseNotAssigned()
        {
            using var driver = _driverFactory.CreateDriver();

            var steps = new List<(string Action, string Result)>();
            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToEngagementLog(driver, steps);

            var caseStatusDropdown = OpenNewFormAndGetCaseStatusDropdown(driver, steps);
            var caseStatusSelect = new SelectElement(caseStatusDropdown);
            caseStatusSelect.SelectByValue("02");
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            steps.Add(("Case status", "Selected option value 02"));

            var noRadio = driver.FindElement(By.CssSelector("input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_rbtnNotAssigned"));
            ClickElement(driver, noRadio);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            steps.Add(("Case assigned radio", "Selected No"));

            var terminationRow = driver.WaitforElementToBeInDOM(By.CssSelector("#trEffortsTerminated"), 10)
                ?? throw new InvalidOperationException("Termination section was not found.");
            Assert.True(terminationRow.Displayed, "Termination section was not displayed.");

            var terminationDateInput = terminationRow.FindElement(By.CssSelector("input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtTerminationDate"));
            Assert.True(terminationDateInput.Displayed, "Termination date input was not visible.");

            var terminationReasonDropdown = driver.FindElement(By.CssSelector("select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlTerminationReason"));
            Assert.True(terminationReasonDropdown.Displayed, "Termination reason dropdown was not visible.");

            steps.Add(("Termination fields", "Termination date and reason displayed when case not assigned"));

            foreach (var step in steps)
            {
                _output.WriteLine($"{step.Action}: {step.Result}");
            }
        }

        [Fact]
        public void CheckingTerminationFieldsAppearWhenCaseStatusThreeSelected()
        {
            using var driver = _driverFactory.CreateDriver();

            var steps = new List<(string Action, string Result)>();
            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToEngagementLog(driver, steps);

            var caseStatusDropdown = OpenNewFormAndGetCaseStatusDropdown(driver, steps);
            var caseStatusSelect = new SelectElement(caseStatusDropdown);
            caseStatusSelect.SelectByValue("03");
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            steps.Add(("Case status", "Selected option value 03"));

            var terminationRow = driver.WaitforElementToBeInDOM(By.CssSelector("#trEffortsTerminated"), 10)
                ?? throw new InvalidOperationException("Termination section was not found.");

            var terminationDateInput = terminationRow.FindElement(By.CssSelector("input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtTerminationDate"));
            var terminationReasonDropdown = driver.FindElement(By.CssSelector("select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlTerminationReason"));

            Assert.True(terminationRow.Displayed, "Termination section was not displayed.");
            Assert.True(terminationDateInput.Displayed, "Termination date input was not visible.");
            Assert.True(terminationReasonDropdown.Displayed, "Termination reason dropdown was not visible.");
            steps.Add(("Termination fields", "Termination date and reason displayed when case status is 3"));

            var caseAssignedSection = driver.WaitforElementToBeInDOM(By.CssSelector("#trAssessmentCompleted"), 5);
            if (caseAssignedSection != null)
            {
                Assert.False(caseAssignedSection.Displayed, "Case assignment section should not be visible when case status is 3.");
            }

            foreach (var step in steps)
            {
                _output.WriteLine($"{step.Action}: {step.Result}");
            }
        }

        private HomePage SignInAsDataEntry(IPookieWebDriver driver)
        {
            driver.Navigate().GoToUrl(_config.AppUrl);
            driver.WaitForReady(30);

            var loginPage = new LoginPage(driver);
            loginPage.SignIn(_config.UserName, _config.Password);

            Assert.True(loginPage.IsSignedIn(), "User was not signed in successfully.");

            var selectRolePage = new SelectRolePage(driver);
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.IsType<HomePage>(landingPage);

            return (HomePage)landingPage;
        }

        private void NavigateToEngagementLog(IPookieWebDriver driver, List<(string Action, string Result)> steps)
        {
            var formsPane = NavigateToFormsTab(driver, TargetPc1Id, steps);

            var engagementLogLink = formsPane.FindElements(By.CssSelector("a#ctl00_ContentPlaceHolder1_ucForms_lnkPA.moreInfo, a[data-formtype='pa'].moreInfo"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Engagement Log link was not found inside the Forms tab.");

            ClickElement(driver, engagementLogLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            steps.Add(("Engagement Log link", "Engagement Log page displayed"));
        }

        private IWebElement NavigateToFormsTab(IPookieWebDriver driver, string targetPc1Id, List<(string Action, string Result)>? steps)
        {
            var navigationBar = driver.WaitforElementToBeInDOM(By.CssSelector(".navbar"), 30)
                ?? throw new InvalidOperationException("Navigation bar was not present on the page.");

            var searchCasesButton = navigationBar.WaitforElementToBeInDOM(By.CssSelector(".btn-group.middle a[href*='SearchCases.aspx']"), 10)
                ?? throw new InvalidOperationException("Search Cases button was not found.");

            searchCasesButton.Click();
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            steps?.Add(("Search Cases button", "Search Cases page displayed"));

            var searchCasesPage = new SearchCasesPage(driver);
            Assert.True(searchCasesPage.IsLoaded, "Search Cases page did not load after clicking the shortcut.");

            var pc1Input = driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='txtPC1ID']"), 5)
                ?? throw new InvalidOperationException("PC1 ID input was not found on the Search Cases page.");

            pc1Input.Clear();
            pc1Input.SendKeys(targetPc1Id);

            var searchButton = driver.WaitforElementToBeInDOM(By.CssSelector("a#ctl00_ContentPlaceHolder1_btSearch"), 5)
                ?? throw new InvalidOperationException("Search button was not found on the Search Cases page.");

            searchButton.Click();
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            steps?.Add(("Search button", $"Search executed for PC1 {targetPc1Id}"));

            var formsTab = driver.WaitforElementToBeInDOM(By.CssSelector("a#formstab[data-toggle='tab'][href='#forms']"), 10)
                ?? throw new InvalidOperationException("Forms tab was not found on the Search Cases results.");
            formsTab.Click();
            driver.WaitForReady(5);

            var formsPane = driver.WaitforElementToBeInDOM(By.CssSelector(".tab-pane#forms"), 5)
                ?? throw new InvalidOperationException("Forms tab content was not found.");
            if (!formsPane.Displayed || !formsPane.GetAttribute("class").Contains("active", StringComparison.OrdinalIgnoreCase))
            {
                formsTab.Click();
                driver.WaitForReady(3);
                formsPane = driver.WaitforElementToBeInDOM(By.CssSelector(".tab-pane#forms"), 5)
                    ?? throw new InvalidOperationException("Forms tab content was not found after activation.");
            }

            var formsSummary = formsPane.Text?.Trim();
            var formsSnippet = string.IsNullOrWhiteSpace(formsSummary)
                ? "(no forms content)"
                : formsSummary.Length > 400 ? formsSummary[..400] + "..." : formsSummary;
            steps?.Add(("Forms tab", formsSnippet));
            return formsPane;
        }

        private IWebElement FindElementInModalOrPage(IPookieWebDriver driver, string cssSelector, string description, int timeoutSeconds = 10)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            Exception? lastException = null;

            while (DateTime.Now <= endTime)
            {
                try
                {
                    var modal = driver.FindElements(By.CssSelector(".modal.show, .modal.in, .modal[style*='display: block'], .modal.fade.in"))
                        .FirstOrDefault(el => el.Displayed);
                    if (modal != null)
                    {
                        var withinModal = modal.FindElements(By.CssSelector(cssSelector))
                            .FirstOrDefault(el => el.Displayed);
                        if (withinModal != null)
                        {
                            _output.WriteLine($"[INFO] Located '{description}' inside modal using selector '{cssSelector}'.");
                            return withinModal;
                        }
                    }

                    var fallback = driver.FindElements(By.CssSelector(cssSelector))
                        .FirstOrDefault(el => el.Displayed);
                    if (fallback != null)
                    {
                        _output.WriteLine($"[INFO] Located '{description}' on page using selector '{cssSelector}'.");
                        return fallback;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(200);
            }

            throw new InvalidOperationException($"'{description}' was not found within the expected time.", lastException);
        }

        private IWebElement EnsureCaseStatusDropdown(IPookieWebDriver driver, List<(string Action, string Result)> steps)
        {
            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _output.WriteLine($"[INFO] Attempting to locate Case Status dropdown (attempt {attempt}/{maxAttempts}).");
                var dropdown = driver.FindElements(By.CssSelector("select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlCaseStatus.form-control"))
                    .FirstOrDefault(el => el.Displayed);
                if (dropdown != null)
                {
                    _output.WriteLine("[INFO] Case Status dropdown located.");
                    return dropdown;
                }

                _output.WriteLine($"[WARN] Case Status dropdown not visible on attempt {attempt}. Re-entering Activity Month and re-clicking Add New.");
                var activityDescription = attempt == 1 ? "Activity month (post advance)" : $"Activity month retry {attempt - 1}";
                var activityInput = FindElementInModalOrPage(
                    driver,
                    "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtActivityMonth.form-control.mon-year",
                    activityDescription,
                    15);
                SetInputValue(driver, activityInput, "11/2025", activityDescription, steps, triggerBlur: true);

                var addNewDescription = attempt == 1 ? "Add New button (post advance)" : $"Add New button retry {attempt - 1}";
                var addNewButton = FindElementInModalOrPage(
                    driver,
                    "input#btnSubmit.btn.btn-primary, button#btnSubmit.btn.btn-primary, .modal-footer .btn-primary",
                    addNewDescription,
                    10);
                ClickElement(driver, addNewButton);
                driver.WaitForUpdatePanel(30);
                driver.WaitForReady(30);
                Thread.Sleep(1500);
                steps.Add((addNewDescription, "Reattempted advance to case status step"));
            }

            throw new InvalidOperationException("Case Status dropdown did not appear after multiple attempts.");
        }

        private IWebElement OpenNewFormAndGetCaseStatusDropdown(IPookieWebDriver driver, List<(string Action, string Result)> steps)
        {
            var newFormButton = driver.FindElements(By.CssSelector("a#btnAdd.btn.btn-default.pull-right"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("New Form button was not found on the Engagement Log page.");

            ClickElement(driver, newFormButton);
            driver.WaitForReady(5);
            steps.Add(("New Form button", "New Form dialog displayed"));

            _output.WriteLine("[INFO] Waiting for activity month input in modal.");
            var activityMonthInput = FindElementInModalOrPage(driver, "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtActivityMonth.form-control.mon-year", "Activity Month input", 15);
            SetInputValue(driver, activityMonthInput, "11/2025", "Activity month", steps, triggerBlur: true);

            var submitButton = FindElementInModalOrPage(
                driver,
                "input#btnSubmit.btn.btn-primary, button#btnSubmit.btn.btn-primary, .modal-footer .btn-primary",
                "Add New button",
                10);

            ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);
            steps.Add(("Add New button", "Advanced to case status step"));

            return EnsureCaseStatusDropdown(driver, steps);
        }

        private static void ClickElement(IPookieWebDriver driver, IWebElement element)
        {
            try
            {
                element.Click();
                return;
            }
            catch (Exception)
            {
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                Thread.Sleep(200);
                js.ExecuteScript("arguments[0].click();", element);
            }
        }

        private void SetInputValue(IPookieWebDriver driver, IWebElement input, string value, string fieldDescription, List<(string Action, string Result)> steps, bool triggerBlur = false)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            try
            {
                _output.WriteLine($"[INFO] Setting '{fieldDescription}' via standard send keys.");
                input.Clear();
                input.SendKeys(value);
            }
            catch (ElementNotInteractableException ex)
            {
                _output.WriteLine($"[WARN] '{fieldDescription}' not interactable. Falling back to JavaScript. Details: {ex.Message}");
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", input, value);
            }

            var finalValue = input.GetAttribute("value")?.Trim() ?? string.Empty;
            if (!string.Equals(finalValue, value, StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine($"[WARN] '{fieldDescription}' value after entry was '{finalValue}'. Retrying via JavaScript.");
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", input, value);
                Thread.Sleep(200);
                finalValue = input.GetAttribute("value")?.Trim() ?? string.Empty;
            }

            if (!string.Equals(finalValue, value, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unable to set '{fieldDescription}' to '{value}'. Last observed value '{finalValue}'.");
            }

            if (triggerBlur)
            {
                try
                {
                    input.SendKeys(Keys.Tab);
                }
                catch (InvalidElementStateException)
                {
                    // ignore, fallback to JS blur
                }

                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('blur', { bubbles: true }));", input);
                Thread.Sleep(200);
            }

            steps.Add((fieldDescription, $"{value} confirmed"));
            _output.WriteLine($"[INFO] '{fieldDescription}' now has value '{finalValue}'.");
        }
    }
}

