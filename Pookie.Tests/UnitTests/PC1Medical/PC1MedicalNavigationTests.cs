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

namespace AFUT.Tests.UnitTests.PC1Medical
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class PC1MedicalNavigationTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;
        private readonly ITestOutputHelper _output;

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public PC1MedicalNavigationTests(AppConfig config, ITestOutputHelper output)
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
        public void ClickingPc1MedicalLinkOpensMedicalInformationForm(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToPc1MedicalPage(driver, formsPane, pc1Id);

            var medicalHeader = driver.FindElements(By.CssSelector(
                    "h1, h2, h3, .panel-title, .page-title, .section-header"))
                .FirstOrDefault(el => el.Displayed &&
                                      el.Text.Contains("PC1 Medical", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(medicalHeader);
            _output.WriteLine($"[PASS] PC1 Medical page header located: {medicalHeader.Text?.Trim()}");

            var pc1Display = CommonTestHelper.FindPc1Display(driver, pc1Id);
            Assert.False(string.IsNullOrWhiteSpace(pc1Display), "Unable to locate PC1 ID on the PC1 Medical Information page.");
            Assert.Contains(pc1Id, pc1Display, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Verified PC1 ID display on medical form: {pc1Display}");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(2)]
        public void SubmittingBlankPc1MedicalFormShowsValidationMessages(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (_, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToPc1MedicalPage(driver, formsPane, pc1Id);
            OpenNewPc1MedicalRecord(driver);
            _output.WriteLine("[INFO] Opened new PC1 Medical form");

            var submitButton = GetPc1MedicalSubmitButton(driver);
            _output.WriteLine("[INFO] Clicking Submit on blank PC1 Medical form");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            var typeError = FindValidationMessage(driver, "Type is required")
                ?? throw new InvalidOperationException("Type validation message was not displayed.");
            _output.WriteLine($"[PASS] Type validation displayed: {typeError.Text?.Trim()}");

            var dateError = FindValidationMessage(driver, "Date is required")
                ?? throw new InvalidOperationException("Date validation message was not displayed.");
            _output.WriteLine($"[PASS] Date validation displayed: {dateError.Text?.Trim()}");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(3)]
        public void SavingPc1MedicalRecordShowsSuccessFeedback(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();
            const string targetDate = "11/30/25";

            var (_, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToPc1MedicalPage(driver, formsPane, pc1Id);
            OpenNewPc1MedicalRecord(driver);

            var (selectedTypeText, _) = SelectRandomMedicalType(driver);
            _output.WriteLine($"[INFO] Selected PC1 Medical type: {selectedTypeText}");

            SetMedicalRecordDate(driver, targetDate);
            SubmitPc1MedicalForm(driver, targetDate, selectedTypeText);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(4)]
        public void EditingPc1MedicalRecordUpdatesGrid(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();
            const string targetDate = "11/30/25";
            const string originalTypeText = "Primary Care Provider";
            const string originalTypeValue = "19";
            const string updatedTypeText = "Urgent Care";
            const string updatedTypeValue = "20";

            var (_, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToPc1MedicalPage(driver, formsPane, pc1Id);
            EnsurePc1MedicalRecordExists(driver, targetDate, originalTypeText, originalTypeValue);

            var targetRow = FindPc1MedicalGridRow(driver, targetDate, originalTypeText)
                ?? throw new InvalidOperationException($"PC1 Medical record for {targetDate} / {originalTypeText} was not found for editing.");

            var editLink = targetRow.FindElements(By.CssSelector("a.btn.btn-default.btnEdit"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Edit button was not found for the PC1 Medical record.");

            _output.WriteLine("[INFO] Opening PC1 Medical record for editing");
            CommonTestHelper.ClickElement(driver, editLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            SelectMedicalType(driver, updatedTypeText, updatedTypeValue);
            SetMedicalRecordDate(driver, targetDate);
            SubmitPc1MedicalForm(driver, targetDate, updatedTypeText);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(5)]
        public void DashboardSummaryCountsMatchGridTotals(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();
            const string targetDate = "11/30/25";

            var (_, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToPc1MedicalPage(driver, formsPane, pc1Id);

            var baselineSummaryCounts = GetPc1MedicalSummaryCounts(driver);
            var baselineGridCounts = GetPc1MedicalGridCounts(driver);
            var createdCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var random = new Random();
            const int iterations = 3;

            for (var i = 0; i < iterations; i++)
            {
                var selectedOption = MedicalTypeOptions[random.Next(MedicalTypeOptions.Length)];

                _output.WriteLine($"[INFO] Creating PC1 Medical record #{i + 1}: {selectedOption.TypeText} on {targetDate}");
                CreatePc1MedicalRecord(driver, selectedOption.TypeText, selectedOption.TypeValue, targetDate);

                createdCounts[selectedOption.TypeText] = GetDictionaryValue(createdCounts, selectedOption.TypeText) + 1;

                var summaryCounts = GetPc1MedicalSummaryCounts(driver);
                var gridCounts = GetPc1MedicalGridCounts(driver);

                foreach (var option in MedicalTypeOptions)
                {
                    var expectedSummaryCount = GetDictionaryValue(baselineSummaryCounts, option.TypeText) +
                                               GetDictionaryValue(createdCounts, option.TypeText);
                    var expectedGridCount = GetDictionaryValue(baselineGridCounts, option.TypeText) +
                                            GetDictionaryValue(createdCounts, option.TypeText);

                    Assert.Equal(expectedSummaryCount, GetDictionaryValue(summaryCounts, option.TypeText));
                    Assert.Equal(expectedGridCount, GetDictionaryValue(gridCounts, option.TypeText));
                }

                _output.WriteLine("[PASS] Dashboard summary matches grid totals after record creation.");
            }
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(6)]
        public void DeletingPc1MedicalRecordUpdatesCounts(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();
            const string targetDate = "11/30/25";
            const string targetTypeText = "Urgent Care";
            const string targetTypeValue = "20";

            var (_, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToPc1MedicalPage(driver, formsPane, pc1Id);
            EnsurePc1MedicalRecordExists(driver, targetDate, targetTypeText, targetTypeValue);

            var summaryBefore = GetPc1MedicalSummaryCounts(driver);
            var gridBefore = GetPc1MedicalGridCounts(driver);
            var initialTypeCount = GetDictionaryValue(gridBefore, targetTypeText);
            Assert.True(initialTypeCount > 0, $"Expected at least one '{targetTypeText}' record before deletion.");

            // Cancel first delete attempt
            _output.WriteLine("[INFO] Opening delete modal and cancelling deletion");
            var cancelModal = OpenPc1MedicalDeleteModal(driver, targetDate, targetTypeText);
            var cancelButton = FindModalElement(cancelModal,
                "button.btn.btn-default[data-dismiss='modal']",
                "button.btn.btn-default",
                ".modal-footer button.btn-default");
            CommonTestHelper.ClickElement(driver, cancelButton);
            WaitForModalToClose(cancelModal);
            driver.WaitForReady(5);
            Thread.Sleep(500);

            var summaryAfterCancel = GetPc1MedicalSummaryCounts(driver);
            var gridAfterCancel = GetPc1MedicalGridCounts(driver);
            Assert.Equal(GetDictionaryValue(summaryBefore, targetTypeText), GetDictionaryValue(summaryAfterCancel, targetTypeText));
            Assert.Equal(initialTypeCount, GetDictionaryValue(gridAfterCancel, targetTypeText));

            // Confirm delete
            _output.WriteLine("[INFO] Confirming delete");
            var confirmModal = OpenPc1MedicalDeleteModal(driver, targetDate, targetTypeText);
            var confirmButton = FindModalElement(confirmModal,
                "a.btn.btn-primary[id*='lbConfirmDelete']",
                ".modal-footer a.btn.btn-primary");
            CommonTestHelper.ClickElement(driver, confirmButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            WaitForModalToClose(confirmModal);

            var toastText = WaitForPc1MedicalDeleteToast(driver);
            _output.WriteLine($"[PASS] Delete toast displayed: {toastText}");

            WaitForPc1MedicalRowRemoval(driver, targetDate, targetTypeText);

            var summaryAfterDelete = GetPc1MedicalSummaryCounts(driver);
            var gridAfterDelete = GetPc1MedicalGridCounts(driver);

            foreach (var option in MedicalTypeOptions)
            {
                var beforeCount = GetDictionaryValue(summaryBefore, option.TypeText);
                var expectedSummary = option.TypeText.Equals(targetTypeText, StringComparison.OrdinalIgnoreCase)
                    ? Math.Max(0, beforeCount - 1)
                    : beforeCount;
                Assert.Equal(expectedSummary, GetDictionaryValue(summaryAfterDelete, option.TypeText));

                var beforeGridCount = GetDictionaryValue(gridBefore, option.TypeText);
                var expectedGrid = option.TypeText.Equals(targetTypeText, StringComparison.OrdinalIgnoreCase)
                    ? Math.Max(0, beforeGridCount - 1)
                    : beforeGridCount;
                Assert.Equal(expectedGrid, GetDictionaryValue(gridAfterDelete, option.TypeText));
            }

            _output.WriteLine("[PASS] Dashboard summary and grid counts decreased by one after deletion.");
        }

        private static readonly MedicalTypeOption[] MedicalTypeOptions =
        {
            new("Ob/Gyn Visit", "01", "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblOBGYN"),
            new("ED (Emergency Room Visit)", "02", "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblEmergency"),
            new("Primary Care Provider", "19", "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblPrimaryCareProvider"),
            new("Urgent Care", "20", "#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_lblUrgentCare"),
        };

        private string BuildExpectedPc1MedicalUrlPrefix(string pc1Id)
        {
            var baseUri = new Uri(_config.AppUrl);
            var root = $"{baseUri.Scheme}://{baseUri.Authority}";
            return $"{root}/Pages/PC1Medical.aspx?pc1id={pc1Id}";
        }

        private void NavigateToPc1MedicalPage(IPookieWebDriver driver, IWebElement formsPane, string pc1Id)
        {
            var pc1MedicalLink = formsPane.FindElements(By.CssSelector(
                    "a.list-group-item.moreInfo[href*='PC1Medical.aspx'], " +
                    "a.moreInfo[data-formtype='pc1med'], " +
                    "a.list-group-item[title*='PC1 Medical']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("PC1 Medical Information link was not found inside the Forms tab.");

            _output.WriteLine($"[INFO] Found PC1 Medical link: {pc1MedicalLink.Text?.Trim()}");

            CommonTestHelper.ClickElement(driver, pc1MedicalLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);

            var currentUrl = driver.Url;
            _output.WriteLine($"[INFO] Current URL after clicking PC1 Medical link: {currentUrl}");

            Assert.Contains("PC1Medical.aspx", currentUrl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(pc1Id, currentUrl, StringComparison.OrdinalIgnoreCase);

            var expectedUrlPrefix = BuildExpectedPc1MedicalUrlPrefix(pc1Id);
            Assert.StartsWith(expectedUrlPrefix, currentUrl, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Verified navigation to PC1 Medical page: {currentUrl}");
        }

        private void OpenNewPc1MedicalRecord(IPookieWebDriver driver)
        {
            var newRecordButton = driver.FindElements(By.CssSelector(
                    "button.btn.btn-default#lnkNewRec, " +
                    "button.btn.btn-default[id*='lnkNewRec'], " +
                    "button.btn.btn-default[data-trigger*='pc1medical']"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("New PC1 Medical", StringComparison.OrdinalIgnoreCase) ?? false))
                ?? throw new InvalidOperationException("New PC1 Medical Record button was not found on the PC1 Medical page.");

            _output.WriteLine($"[INFO] Found New PC1 Medical Record button: {newRecordButton.Text?.Trim()}");

            CommonTestHelper.ClickElement(driver, newRecordButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
        }

        private IWebElement GetPc1MedicalSubmitButton(IPookieWebDriver driver)
        {
            return WebElementHelper.FindElementInModalOrPage(
                driver,
                "button.btn.btn-primary#btnSubmit, " +
                "button.btn.btn-primary[id*='btnSubmit'], " +
                "a.btn.btn-primary[title*='Submit']",
                "PC1 Medical Submit button",
                20);
        }

        private IWebElement? FindValidationMessage(IPookieWebDriver driver, string expectedText)
        {
            return driver.FindElements(By.CssSelector(
                    ".text-danger, span.text-danger, span.validation-error, span.field-validation-error, " +
                    "div.validation-summary-errors li, span[style*='color: red'], span[style*='color:red']"))
                .FirstOrDefault(el =>
                {
                    if (!el.Displayed || string.IsNullOrWhiteSpace(el.Text))
                    {
                        return false;
                    }

                    var normalized = el.Text.Replace("*", string.Empty).Trim();
                    return normalized.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
                });
        }

        private (string optionText, string optionValue) SelectRandomMedicalType(IPookieWebDriver driver)
        {
            var dropdown = WebElementHelper.FindElementInModalOrPage(
                driver,
                "select.form-control[id*='ddlMedicalItemCode'], select.form-control[name*='ddlMedicalItemCode']",
                "PC1 Medical Type dropdown",
                20);

            var select = new SelectElement(dropdown);
            var validOptions = select.Options
                .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")))
                .Select(opt => new
                {
                    Text = opt.Text.Trim(),
                    Value = opt.GetAttribute("value")
                })
                .Where(opt => !string.Equals(opt.Text, "--Select--", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!validOptions.Any())
            {
                throw new InvalidOperationException("No valid options were found in the PC1 Medical Type dropdown.");
            }

            var random = new Random();
            var chosen = validOptions[random.Next(validOptions.Count)];

            WebElementHelper.SelectDropdownOption(driver, dropdown, "PC1 Medical Type dropdown", chosen.Text, chosen.Value);
            return (chosen.Text, chosen.Value);
        }

        private void SelectMedicalType(IPookieWebDriver driver, string typeText, string? typeValue)
        {
            var dropdown = WebElementHelper.FindElementInModalOrPage(
                driver,
                "select.form-control[id*='ddlMedicalItemCode'], select.form-control[name*='ddlMedicalItemCode']",
                "PC1 Medical Type dropdown",
                20);

            WebElementHelper.SelectDropdownOption(driver, dropdown, "PC1 Medical Type dropdown", typeText, typeValue);
        }

        private void SetMedicalRecordDate(IPookieWebDriver driver, string dateText)
        {
            var dateInput = WebElementHelper.FindElementInModalOrPage(
                driver,
                "div.input-group.date input.form-control, input.form-control[id*='txtPC1ItemDate'], input.form-control[name*='txtPC1ItemDate']",
                "PC1 Medical Date input",
                20);

            WebElementHelper.SetInputValue(driver, dateInput, dateText, "PC1 Medical Date", triggerBlur: true);
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);
        }

        private IWebElement? FindInlineSaveMessage(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector(
                    "div#div_rec_msg .save_rec_msg, span.save_rec_msg, span[id*='lblsave_rec_msg']"))
                .FirstOrDefault(el => el.Displayed && !string.IsNullOrWhiteSpace(el.Text));
        }

        private IWebElement? FindPc1MedicalGridRow(IPookieWebDriver driver, string expectedDateText, string expectedTypeText)
        {
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector(
                    "table.table.table-condensed.data-display, table[id*='grPC1MedicalRecords']"),
                20);

            if (grid == null)
            {
                return null;
            }

            var rows = grid.FindElements(By.CssSelector("tbody tr"))
                .Where(tr => tr.Displayed && !string.IsNullOrWhiteSpace(tr.Text))
                .ToList();

            foreach (var row in rows)
            {
                var rowText = row.Text ?? string.Empty;
                if (rowText.Contains(expectedDateText, StringComparison.OrdinalIgnoreCase) &&
                    rowText.Contains(expectedTypeText, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return null;
        }

        private void CreatePc1MedicalRecord(IPookieWebDriver driver, string typeText, string typeValue, string dateText)
        {
            OpenNewPc1MedicalRecord(driver);
            SelectMedicalType(driver, typeText, typeValue);
            SetMedicalRecordDate(driver, dateText);
            SubmitPc1MedicalForm(driver, dateText, typeText);
        }

        private void SubmitPc1MedicalForm(IPookieWebDriver driver, string expectedDateText, string expectedTypeText)
        {
            var submitButton = GetPc1MedicalSubmitButton(driver);
            _output.WriteLine("[INFO] Submitting PC1 Medical form");
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1500);

            var toastMessage = WebElementHelper.GetToastMessage(driver, 1500);
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Toast message did not appear after saving PC1 Medical record.");
            Assert.Contains("Record Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("successfully saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Toast message displayed: {toastMessage}");

            var inlineMessage = FindInlineSaveMessage(driver)
                ?? throw new InvalidOperationException("Inline save confirmation message was not displayed.");
            var inlineText = inlineMessage.Text?.Trim() ?? string.Empty;
            Assert.Contains("Record has been saved", inlineText, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Inline save message displayed: {inlineText}");

            driver.WaitForReady(10);
            driver.WaitForUpdatePanel(10);
            Thread.Sleep(500);

            var gridRow = FindPc1MedicalGridRow(driver, expectedDateText, expectedTypeText)
                ?? throw new InvalidOperationException($"PC1 Medical record ({expectedDateText}, {expectedTypeText}) did not appear in the grid.");
            _output.WriteLine($"[PASS] Grid row located: {gridRow.Text}");
        }

        private void EnsurePc1MedicalRecordExists(IPookieWebDriver driver, string dateText, string typeText, string typeValue)
        {
            var existingRow = FindPc1MedicalGridRow(driver, dateText, typeText);
            if (existingRow != null)
            {
                _output.WriteLine($"[INFO] Existing PC1 Medical record already present for {dateText} / {typeText}.");
                return;
            }

            _output.WriteLine($"[INFO] Creating PC1 Medical record for {dateText} / {typeText}.");
            OpenNewPc1MedicalRecord(driver);
            SelectMedicalType(driver, typeText, typeValue);
            SetMedicalRecordDate(driver, dateText);
            SubmitPc1MedicalForm(driver, dateText, typeText);
        }

        private Dictionary<string, int> GetPc1MedicalSummaryCounts(IPookieWebDriver driver)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in MedicalTypeOptions)
            {
                var label = driver.FindElements(By.CssSelector(option.SummarySelector))
                    .FirstOrDefault(el => el.Displayed) ??
                    driver.FindElements(By.CssSelector(option.SummarySelector)).FirstOrDefault();

                var raw = label?.Text?.Trim() ?? "0";
                counts[option.TypeText] = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : 0;
            }

            return counts;
        }

        private Dictionary<string, int> GetPc1MedicalGridCounts(IPookieWebDriver driver)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var grid = driver.WaitforElementToBeInDOM(By.CssSelector(
                    "table.table.table-condensed.data-display, table[id*='grPC1MedicalRecords']"),
                20);

            if (grid == null)
            {
                return counts;
            }

            var rows = grid.FindElements(By.CssSelector("tbody tr"))
                .Where(tr => tr.Displayed && tr.FindElements(By.CssSelector("td")).Count >= 3)
                .ToList();

            foreach (var row in rows)
            {
                var cells = row.FindElements(By.CssSelector("td"));
                if (cells.Count < 3)
                {
                    continue;
                }

                var typeText = cells[2].Text?.Trim();
                if (string.IsNullOrWhiteSpace(typeText))
                {
                    continue;
                }

                counts[typeText] = GetDictionaryValue(counts, typeText) + 1;
            }

            return counts;
        }

        private static int GetDictionaryValue(IDictionary<string, int> source, string key)
        {
            return source.TryGetValue(key, out var value) ? value : 0;
        }

        private IWebElement OpenPc1MedicalDeleteModal(IPookieWebDriver driver, string dateText, string typeText, int timeoutSeconds = 10)
        {
            var row = WaitForPc1MedicalRow(driver, dateText, typeText);
            var deleteButton = row.FindElements(By.CssSelector("a.btn.btn-danger"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Delete button was not found for the PC1 Medical record.");

            CommonTestHelper.ClickElement(driver, deleteButton);

            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= end)
            {
                var modal = driver.FindElements(By.CssSelector("div.dc-confirmation-modal.modal"))
                    .FirstOrDefault(IsModalDisplayed);
                if (modal != null)
                {
                    return modal;
                }

                Thread.Sleep(200);
            }

            throw new InvalidOperationException("Delete confirmation modal did not appear.");
        }

        private IWebElement WaitForPc1MedicalRow(IPookieWebDriver driver, string dateText, string typeText, int timeoutSeconds = 10)
        {
            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= end)
            {
                var row = FindPc1MedicalGridRow(driver, dateText, typeText);
                if (row != null)
                {
                    return row;
                }

                Thread.Sleep(250);
            }

            throw new InvalidOperationException($"PC1 Medical record ({dateText}, {typeText}) did not appear.");
        }

        private void WaitForPc1MedicalRowRemoval(IPookieWebDriver driver, string dateText, string typeText, int timeoutSeconds = 15)
        {
            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= end)
            {
                var row = FindPc1MedicalGridRow(driver, dateText, typeText);
                if (row == null)
                {
                    return;
                }

                Thread.Sleep(300);
            }

            throw new InvalidOperationException($"PC1 Medical record ({dateText}, {typeText}) was still present after deletion.");
        }

        private void WaitForModalToClose(IWebElement modal, int timeoutSeconds = 10)
        {
            var end = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now <= end)
            {
                if (!IsModalDisplayed(modal))
                {
                    return;
                }

                Thread.Sleep(200);
            }

            throw new InvalidOperationException("Delete confirmation modal did not close.");
        }

        private static bool IsModalDisplayed(IWebElement? modal)
        {
            if (modal == null)
            {
                return false;
            }

            try
            {
                if (modal.Displayed)
                {
                    return true;
                }
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }

            var classAttr = modal.GetAttribute("class") ?? string.Empty;
            if (classAttr.Contains("in", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var style = modal.GetAttribute("style") ?? string.Empty;
            return style.Contains("display: block", StringComparison.OrdinalIgnoreCase);
        }

        private static IWebElement FindModalElement(IWebElement modal, params string[] selectors)
        {
            foreach (var selector in selectors)
            {
                var element = modal.FindElements(By.CssSelector(selector))
                    .FirstOrDefault(el => el.Displayed);
                if (element != null)
                {
                    return element;
                }
            }

            throw new InvalidOperationException("Expected element was not found inside the modal.");
        }

        private string WaitForPc1MedicalDeleteToast(IPookieWebDriver driver, int timeoutSeconds = 15)
        {
            var toast = driver.WaitforElementToBeInDOM(By.CssSelector(".jq-toast-single"), timeoutSeconds)
                ?? throw new InvalidOperationException("Delete toast was not displayed for PC1 Medical record.");

            var toastText = toast.Text?.Trim() ?? string.Empty;
            Assert.Contains("PC1 Medical", toastText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("deleted", toastText, StringComparison.OrdinalIgnoreCase);
            return toastText;
        }

        private record MedicalTypeOption(string TypeText, string TypeValue, string SummarySelector);
    }
}

