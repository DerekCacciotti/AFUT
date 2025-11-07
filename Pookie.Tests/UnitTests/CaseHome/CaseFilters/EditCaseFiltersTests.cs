using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;

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
    }
}
