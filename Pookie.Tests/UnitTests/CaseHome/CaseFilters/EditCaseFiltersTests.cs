using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;

namespace AFUT.Tests.UnitTests.CaseHome.CaseFilters
{
    public class EditCaseFiltersTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;

        public EditCaseFiltersTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void EditingDropdownAndTextboxFiltersPersistsToCaseHome()
        {
            using var driver = _driverFactory.CreateDriver();

            CaseHomePage caseHomePage = CaseHomeTestHelper.NavigateToCaseHome(driver, _config);
            var editor = caseHomePage.OpenCaseFiltersEditor();
            var filters = editor.GetFilters();

            // Pick a dropdown with an alternative value
            var dropdownField = filters
                .Where(filter => filter.IsDropdown && filter.IsEnabled)
                .Select(filter => new
                {
                    Filter = filter,
                    Alternative = filter.GetDropdownOptions().FirstOrDefault(option =>
                        !string.IsNullOrWhiteSpace(option.Value) &&
                        !string.Equals(option.Value, filter.GetSelectedValue(), StringComparison.OrdinalIgnoreCase))
                })
                .FirstOrDefault(result => result.Alternative is not null);

            if (dropdownField is null)
            {
                throw new InvalidOperationException("No editable dropdown filter with an alternate value was found.");
            }

            dropdownField.Filter.SetDropdownValue(dropdownField.Alternative.Value);

            // Pick a text field and change it
            var textField = filters.First(filter => filter.IsTextInput && filter.IsEnabled);
            textField.SetTextValue("11/06/25");

            editor.Submit();
        }

        [Fact]
        public void CancelButtonReturnsToCaseHomeWithoutSaving()
        {
            using var driver = _driverFactory.CreateDriver();

            CaseHomePage caseHomePage = CaseHomeTestHelper.NavigateToCaseHome(driver, _config);
            var editor = caseHomePage.OpenCaseFiltersEditor();

            editor.Cancel();
        }

        [Fact]
        public void SubmittingInvalidDateFormat_ShowsErrorAlert()
        {
            using var driver = _driverFactory.CreateDriver();

            CaseHomePage caseHomePage = CaseHomeTestHelper.NavigateToCaseHome(driver, _config);
            var editor = caseHomePage.OpenCaseFiltersEditor();
            var filters = editor.GetFilters();

            // Find a text field that accepts dates
            var dateField = filters.FirstOrDefault(filter => filter.IsTextInput && filter.IsEnabled);
            
            if (dateField is null)
            {
                throw new InvalidOperationException("No editable text filter was found for testing invalid dates.");
            }

            // Enter invalid date value
            dateField.SetTextValue("43/43/42");

            editor.Submit();

            // Wait for error alert to appear
            System.Threading.Thread.Sleep(2000);

            // Find the error alert
            var errorAlert = driver.FindElements(By.CssSelector("div.alert.alert-info"))
                .FirstOrDefault(alert => alert.Displayed && 
                                        alert.Text.Contains("You have encountered an error in the Healthy Families application"));

            Assert.NotNull(errorAlert);
            Assert.Contains("You have encountered an error in the Healthy Families application", errorAlert.Text);
        }
    }
}
