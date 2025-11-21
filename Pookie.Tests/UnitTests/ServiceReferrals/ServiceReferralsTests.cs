using System;
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

namespace AFUT.Tests.UnitTests.ServiceReferrals
{
    public class ServiceReferralsTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;
        private const string TargetPc1Id = "EC01001408989";

        public ServiceReferralsTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void CheckingTheAddNewOfServiceReferralForm()
        {
            using var driver = _driverFactory.CreateDriver();

            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToServiceReferrals(driver);

            var pc1Display = FindPc1Display(driver, TargetPc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on Service Referrals page.");
            Assert.Contains(TargetPc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);

            CreateNewReferralEntry(driver);
        }

        [Fact]
        public void CheckingServiceReferralFormValidationAndSubmission()
        {
            using var driver = _driverFactory.CreateDriver();

            var homePage = SignInAsDataEntry(driver);

            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToServiceReferrals(driver);

            var pc1Display = FindPc1Display(driver, TargetPc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on Service Referrals page.");
            Assert.Contains(TargetPc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);

            CreateNewReferralEntry(driver);

            var requiredMessages = new[]
            {
                "Worker Required",
                "Service Code Required",
                "Family Member Referred Required",
                "Nature of Referral Required",
                "Agency referred to is Required"
            };

            var validationText = SubmitAndCaptureValidation(driver);
            Assert.False(string.IsNullOrWhiteSpace(validationText));
            foreach (var message in requiredMessages)
            {
                Assert.Contains(message, validationText, StringComparison.OrdinalIgnoreCase);
            }

            SelectWorker(driver, "Test, Derek", "3489");
            validationText = SubmitAndCaptureValidation(driver);
            AssertValidationMessageCleared(validationText, "Worker Required");

            SelectServiceCode(driver, "02 Child primary care", "02");
            validationText = SubmitAndCaptureValidation(driver);
            AssertValidationMessageCleared(validationText, "Service Code Required");

            SelectFamilyMemberReferred(driver, "2. Primary Caretaker 2", "02");
            validationText = SubmitAndCaptureValidation(driver);
            AssertValidationMessageCleared(validationText, "Family Member Referred Required");

            SelectNatureOfReferral(driver, "2. Inform/Discuss", "02");
            validationText = SubmitAndCaptureValidation(driver);
            AssertValidationMessageCleared(validationText, "Nature of Referral Required");

            SelectAgencyReferredTo(driver, "Anonymized", "1");
            validationText = SubmitAndCaptureValidation(driver);
            Assert.True(string.IsNullOrWhiteSpace(validationText));

            WaitForToastMessage(driver, TargetPc1Id);

            var referralRow = FindServiceReferralRow(driver, "11/20/25", "Anonymized");
            Assert.NotNull(referralRow);
        }

        [Fact]
        public void CheckingServiceReferralConditionalFields()
        {
            using var driver = _driverFactory.CreateDriver();

            var homePage = SignInAsDataEntry(driver);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");

            NavigateToServiceReferrals(driver);

            var pc1Display = FindPc1Display(driver, TargetPc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on Service Referrals page.");
            Assert.Contains(TargetPc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);

            CreateNewReferralEntry(driver);
            PopulateMinimumRequiredServiceReferralFields(driver);

            SelectServicesReceived(driver, true);
            var startDateInput = FindElementInModalOrPage(
                driver,
                "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_startdate",
                "Start date input",
                10);
            Assert.True(startDateInput.Displayed);

            SetInputValue(driver, startDateInput, "11/21/25", "Start date", triggerBlur: true);
            Assert.False(IsElementDisplayed(driver, "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_reasonnotreceived"));

            SelectServicesReceived(driver, false);
            var reasonDropdown = FindElementInModalOrPage(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_reasonnotreceived",
                "Reason not received dropdown",
                10);
            Assert.True(reasonDropdown.Displayed);
            SelectDropdownOption(driver, reasonDropdown, "Reason not received dropdown", "2. Participant not eligible for service", "02");

            Assert.False(IsElementDisplayed(driver, "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_startdate"));
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

        private void NavigateToServiceReferrals(IPookieWebDriver driver)
        {
            var formsPane = NavigateToFormsTab(driver, TargetPc1Id);

            var serviceReferralLink = formsPane.FindElements(By.CssSelector("a#ctl00_ContentPlaceHolder1_ucForms_lnkServiceReferral.moreInfo, a[data-formtype='sr'].moreInfo"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Service Referrals link was not found inside the Forms tab.");

            ClickElement(driver, serviceReferralLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
        }

        private void CreateNewReferralEntry(IPookieWebDriver driver)
        {
            var newReferralButton = driver.FindElements(By.CssSelector("a#btnAdd.btn.btn-default.pull-right"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("New Referral button was not found on the Service Referrals page.");

            ClickElement(driver, newReferralButton);
            driver.WaitForReady(5);

            var referralDateInput = FindElementInModalOrPage(
                driver,
                "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtReferralDate.form-control, " +
                "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtServiceDate.form-control, " +
                "input#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_txtServiceReferralDate.form-control, " +
                "input.mon-year, input.date, input[type='date']",
                "Referral date input",
                15);

            SetInputValue(driver, referralDateInput, "11/20/25", "Referral date", triggerBlur: true);

            var addNewButton = FindElementInModalOrPage(
                driver,
                "a#btnSubmit.btn.btn-primary, input#btnSubmit.btn.btn-primary, button#btnSubmit.btn.btn-primary, .modal-footer .btn-primary",
                "Add New button",
                10);

            ClickElement(driver, addNewButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);
        }

        private void SelectWorker(IPookieWebDriver driver, string workerText, string workerValue)
        {
            SelectDropdownOption(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlWorker, " +
                "select[id$='_ddlWorker'], select[id*='ddlCaseWorker'], select[id*='ddlFSW']",
                "Worker dropdown",
                workerText,
                workerValue);
        }

        private void SelectServiceCode(IPookieWebDriver driver, string optionText, string optionValue)
        {
            SelectDropdownOption(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_servicecode, " +
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlServiceCode, " +
                "select[id$='_ddlServiceCode'], select[id$='servicecode'], select[id*='ddlService']",
                "Service code dropdown",
                optionText,
                optionValue);
        }

        private void SelectFamilyMemberReferred(IPookieWebDriver driver, string optionText, string optionValue)
        {
            SelectDropdownOption(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_familymember, " +
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlFamilyMember, " +
                "select[id$='_familymember'], select[id$='_ddlFamilyMember'], select[id*='familymember']",
                "Family member referred dropdown",
                optionText,
                optionValue);
        }

        private void SelectNatureOfReferral(IPookieWebDriver driver, string optionText, string? optionValue)
        {
            SelectDropdownOption(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlNatureOfReferral, " +
                "select[id$='_ddlNatureOfReferral'], select[id*='ddlNature']",
                "Nature of referral dropdown",
                optionText,
                optionValue);
        }

        private void SelectAgencyReferredTo(IPookieWebDriver driver, string optionText, string? optionValue)
        {
            SelectDropdownOption(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlAgency, " +
                "select[id$='_ddlAgency'], select[id*='ddlAgency']",
                "Agency referred to dropdown",
                optionText,
                optionValue);
        }

        private void SelectServicesReceived(IPookieWebDriver driver, bool servicesReceived)
        {
            var dropdown = FindElementInModalOrPage(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_servicesreceived, select[id$='_servicesreceived']",
                "Services received dropdown",
                10);

            var select = new SelectElement(dropdown);
            select.SelectByValue(servicesReceived ? "1" : "0");

            driver.WaitForUpdatePanel(10);
            driver.WaitForReady(10);
            Thread.Sleep(500);
        }

        private void SelectDropdownOption(IPookieWebDriver driver, string cssSelector, string description, string optionText, string? optionValue)
        {
            var dropdown = FindElementInModalOrPage(driver, cssSelector, description, 15);
            SelectDropdownOption(driver, dropdown, description, optionText, optionValue);
        }

        private void SelectDropdownOption(IPookieWebDriver driver, IWebElement dropdown, string description, string optionText, string? optionValue)
        {
            var select = new SelectElement(dropdown);
            SelectByTextOrValue(select, optionText, optionValue);
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);
        }

        private static void SelectByTextOrValue(SelectElement selectElement, string optionText, string? optionValue)
        {
            try
            {
                selectElement.SelectByText(optionText);
                return;
            }
            catch (NoSuchElementException)
            {
                // try value next
            }

            if (!string.IsNullOrWhiteSpace(optionValue))
            {
                selectElement.SelectByValue(optionValue);
                return;
            }

            throw new InvalidOperationException($"Option '{optionText}' was not found in dropdown '{selectElement.WrappedElement?.GetAttribute("id")}'.");
        }

        private string? SubmitAndCaptureValidation(IPookieWebDriver driver)
        {
            ClickSubmitButton(driver);
            return GetValidationSummaryText(driver);
        }

        private void ClickSubmitButton(IPookieWebDriver driver)
        {
            var submitButton = FindElementInModalOrPage(
                driver,
                "a#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_Submit1_LoginView1_btnSubmit.btn.btn-primary, " +
                "a[id$='_Submit1_LoginView1_btnSubmit'].btn.btn-primary, " +
                "a#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_Submit1_btnSubmit.btn.btn-primary, " +
                "button[id$='_btnSubmit'].btn.btn-primary",
                "Service referral Submit button",
                15);

            ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
        }

        private static void AssertValidationMessageCleared(string? validationText, string message)
        {
            if (string.IsNullOrWhiteSpace(validationText))
            {
                return;
            }

            Assert.DoesNotContain(message, validationText, StringComparison.OrdinalIgnoreCase);
        }

        private string? GetValidationSummaryText(IPookieWebDriver driver)
        {
            var summary = driver.FindElements(By.CssSelector("#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ValidationSummary1.validation-summary, " +
                                                              ".validation-summary.alert.alert-danger"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));

            return summary?.Text.Trim();
        }

        private void WaitForToastMessage(IPookieWebDriver driver, string pc1Id)
        {
            var toast = driver.WaitforElementToBeInDOM(By.CssSelector(".jq-toast-single.jq-icon-success"), 15)
                ?? throw new InvalidOperationException("Success toast was not displayed after submitting Service Referral.");

            var toastText = toast.Text?.Trim() ?? string.Empty;
            Assert.Contains("Form Saved", toastText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, toastText, StringComparison.OrdinalIgnoreCase);
        }

        private IWebElement? FindServiceReferralRow(IPookieWebDriver driver, string formDateText, string detailText)
        {
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector("#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_grServiceReferrals, " +
                                                                     "table[id*='grServiceReferral'], table[id*='gvServiceReferral']"), 20);
            if (grid == null)
            {
                return null;
            }

            var rows = grid.FindElements(By.CssSelector("tr")).Where(tr => tr.Displayed && tr.FindElements(By.CssSelector("td")).Any()).ToList();
            foreach (var row in rows)
            {
                var rowText = row.Text ?? string.Empty;
                var dateMatch = rowText.IndexOf(formDateText, StringComparison.OrdinalIgnoreCase) >= 0;
                var detailMatch = rowText.IndexOf(detailText, StringComparison.OrdinalIgnoreCase) >= 0;
                if (dateMatch && detailMatch)
                {
                    return row;
                }
            }

            return null;
        }

        private void PopulateMinimumRequiredServiceReferralFields(IPookieWebDriver driver)
        {
            SelectWorker(driver, "Test, Derek", "3489");
            SelectServiceCode(driver, "02 Child primary care", "02");
            SelectFamilyMemberReferred(driver, "2. Primary Caretaker 2", "02");
            SelectNatureOfReferral(driver, "2. Inform/Discuss", "02");
            SelectAgencyReferredTo(driver, "Anonymized", "1");
        }

        private bool IsElementDisplayed(IPookieWebDriver driver, string cssSelector)
        {
            return driver.FindElements(By.CssSelector(cssSelector))
                .Any(el => el.Displayed);
        }

        private IWebElement NavigateToFormsTab(IPookieWebDriver driver, string targetPc1Id)
        {
            var navigationBar = driver.WaitforElementToBeInDOM(By.CssSelector(".navbar"), 30)
                ?? throw new InvalidOperationException("Navigation bar was not present on the page.");

            var searchCasesButton = navigationBar.WaitforElementToBeInDOM(By.CssSelector(".btn-group.middle a[href*='SearchCases.aspx']"), 10)
                ?? throw new InvalidOperationException("Search Cases button was not found.");

            searchCasesButton.Click();
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);

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
            return formsPane;
        }

        private static string? FindPc1Display(IPookieWebDriver driver, string targetPc1Id)
        {
            var pc1IdDisplay = driver.FindElements(By.CssSelector("[id$='lblPC1ID'], [id$='lblPc1Id'], .pc1-id, .pc1-id-value"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text))
                .Select(el => el.Text.Trim())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(pc1IdDisplay))
            {
                return pc1IdDisplay;
            }

            return driver.FindElements(By.CssSelector(".panel-body, .card-body, .form-group, .list-group, .list-group-item, .row"))
                .Where(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text) && el.Text.Contains(targetPc1Id, StringComparison.OrdinalIgnoreCase))
                .Select(el => el.Text.Trim())
                .FirstOrDefault();
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

        private void SetInputValue(IPookieWebDriver driver, IWebElement input, string value, string fieldDescription, bool triggerBlur = false)
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

                if (!string.Equals(finalValue, value, StringComparison.OrdinalIgnoreCase))
                {
                    js.ExecuteScript("arguments[0].removeAttribute('readonly');", input);
                    input.Clear();
                    js.ExecuteScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", input, value);
                    Thread.Sleep(200);
                    finalValue = input.GetAttribute("value")?.Trim() ?? string.Empty;
                }
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
                }

                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('blur', { bubbles: true }));", input);
                Thread.Sleep(200);
            }

            _output.WriteLine($"[INFO] '{fieldDescription}' now has value '{finalValue}'.");
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
    }
}

