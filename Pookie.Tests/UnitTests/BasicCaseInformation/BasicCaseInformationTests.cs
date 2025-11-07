using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using AFUT.Tests.Routine.SearchCases;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AFUT.Tests.UnitTests.BasicCaseInformation
{
    public class BasicCaseInformationTests : IClassFixture<AppConfig>
    {
        private readonly AppConfig _config;
        private readonly IPookieDriverFactory _driverFactory;

        private const string KnownPc1Id = "AB12010361993";
        private const string KnownPc1FirstName = "Anonymized";
        private const string KnownPc1LastName = "Anonymized";
        private const string KnownTcDob = "060920";
        private const string KnownWorkerDisplayText = "3396, Worker";
        private const string KnownAlternateId = "Anonymized";

        public BasicCaseInformationTests(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Fact]
        public void EditInformationButton_EnablesAllEditableFields()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var basicInfoPage = caseHomePage.OpenBasicInformationEditor();

            Assert.NotNull(basicInfoPage);
            Assert.True(basicInfoPage.IsLoaded, "Basic Case Information page did not load successfully.");

            basicInfoPage.EnterEditMode();

            foreach (var field in basicInfoPage.EditableFields)
            {
                Assert.True(basicInfoPage.IsFieldEditable(field), $"Field '{field}' was not editable after clicking Edit Information.");
            }
        }

        [Fact]
        public void EditInformation_AllEditableFieldsCanBeEditedAndReverted()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var basicInfoPage = caseHomePage.OpenBasicInformationEditor();

            Assert.NotNull(basicInfoPage);
            Assert.True(basicInfoPage.IsLoaded, "Basic Case Information page did not load successfully.");

            basicInfoPage.EnterEditMode();

            foreach (var field in basicInfoPage.EditableFields)
            {
                var originalValue = basicInfoPage.GetFieldValue(field);
                var newValue = GenerateReplacementValue(field, originalValue);

                if (ValuesAreEquivalent(field, originalValue, newValue))
                {
                    newValue = GenerateAlternateReplacementValue(field, originalValue);
                }

                basicInfoPage.SetFieldValue(field, newValue);
                var updatedValue = basicInfoPage.GetFieldValue(field);

                AssertValuesEqual(field, newValue, updatedValue, $"Field '{field}' did not reflect the new value after editing.");

                basicInfoPage.SetFieldValue(field, originalValue);
                var revertedValue = basicInfoPage.GetFieldValue(field);

                AssertValuesEqual(field, originalValue, revertedValue, $"Field '{field}' did not revert to its original value after editing test.");
            }
        }

        [Fact]
        public void EditInformation_Submit_SavesChanges()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var basicInfoPage = caseHomePage.OpenBasicInformationEditor();

            Assert.NotNull(basicInfoPage);
            Assert.True(basicInfoPage.IsLoaded, "Basic Case Information page did not load successfully.");

            var desiredUpdates = new Dictionary<BasicCaseInformationPage.BasicCaseInformationField, string>
            {
                { BasicCaseInformationPage.BasicCaseInformationField.AlternateId, "Anonymized1" },
                { BasicCaseInformationPage.BasicCaseInformationField.ScreenDate, "10/30/19" },
                { BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob, "06/09/20" },
                { BasicCaseInformationPage.BasicCaseInformationField.IntakeDate, "01/07/20" },
                { BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate, "12/17/19" }
            };

            var originalValues = basicInfoPage.EditableFields.ToDictionary(field => field, field => basicInfoPage.GetFieldValue(field));
            CaseHomePage? caseHomeAfterSubmit = null;

            try
            {
                basicInfoPage.EnterEditMode();

                foreach (var update in desiredUpdates)
                {
                    basicInfoPage.SetFieldValue(update.Key, update.Value);
                }

                caseHomeAfterSubmit = basicInfoPage.SubmitChanges();

                var summaryAfterSubmit = caseHomeAfterSubmit.GetBasicInformationSummary();
                AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.AlternateId, desiredUpdates[BasicCaseInformationPage.BasicCaseInformationField.AlternateId], summaryAfterSubmit.AlternateId,
                    "Alternate ID summary value did not reflect the submitted value.");
                AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.ScreenDate, desiredUpdates[BasicCaseInformationPage.BasicCaseInformationField.ScreenDate], summaryAfterSubmit.ScreenDate,
                    "Screen Date summary value did not reflect the submitted value.");
                AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob, desiredUpdates[BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob], summaryAfterSubmit.TargetChildDob,
                    "Target Child DOB summary value did not reflect the submitted value.");
                AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.IntakeDate, desiredUpdates[BasicCaseInformationPage.BasicCaseInformationField.IntakeDate], summaryAfterSubmit.IntakeDate,
                    "Intake Date summary value did not reflect the submitted value.");
                AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate, desiredUpdates[BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate], summaryAfterSubmit.ParentSurveyDate,
                    "Parent Survey Date summary value did not reflect the submitted value.");
            }
            finally
            {
                try
                {
                    var cleanupEditor = (caseHomeAfterSubmit?.OpenBasicInformationEditor()) ?? NavigateToCaseHome(driver).OpenBasicInformationEditor();
                    cleanupEditor.EnterEditMode();

                    foreach (var original in originalValues)
                    {
                        cleanupEditor.SetFieldValue(original.Key, original.Value);
                    }

                    var caseHomeAfterRevert = cleanupEditor.SubmitChanges();
                    var summaryAfterRevert = caseHomeAfterRevert.GetBasicInformationSummary();

                    AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.AlternateId, originalValues[BasicCaseInformationPage.BasicCaseInformationField.AlternateId], summaryAfterRevert.AlternateId,
                        "Alternate ID summary value did not revert to the original value after cleanup.");
                    AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.ScreenDate, originalValues[BasicCaseInformationPage.BasicCaseInformationField.ScreenDate], summaryAfterRevert.ScreenDate,
                        "Screen Date summary value did not revert to the original value after cleanup.");
                    AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob, originalValues[BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob], summaryAfterRevert.TargetChildDob,
                        "Target Child DOB summary value did not revert to the original value after cleanup.");
                    AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.IntakeDate, originalValues[BasicCaseInformationPage.BasicCaseInformationField.IntakeDate], summaryAfterRevert.IntakeDate,
                        "Intake Date summary value did not revert to the original value after cleanup.");
                    AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate, originalValues[BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate], summaryAfterRevert.ParentSurveyDate,
                        "Parent Survey Date summary value did not revert to the original value after cleanup.");
                }
                catch (Exception revertEx)
                {
                    throw new InvalidOperationException("Failed to restore original Basic Case Information values after submit test.", revertEx);
                }
            }
        }

        [Fact]
        public void EditInformation_Cancel_RevertsValues()
        {
            using var driver = _driverFactory.CreateDriver();

            var caseHomePage = NavigateToCaseHome(driver);
            var summaryBefore = caseHomePage.GetBasicInformationSummary();
            var basicInfoPage = caseHomePage.OpenBasicInformationEditor();

            Assert.NotNull(basicInfoPage);
            Assert.True(basicInfoPage.IsLoaded, "Basic Case Information page did not load successfully.");

            var originalValues = basicInfoPage.EditableFields.ToDictionary(field => field, field => basicInfoPage.GetFieldValue(field));

            basicInfoPage.EnterEditMode();

            foreach (var field in basicInfoPage.EditableFields)
            {
                var replacement = GenerateReplacementValue(field, originalValues[field]);
                if (ValuesAreEquivalent(field, replacement, originalValues[field]))
                {
                    replacement = GenerateAlternateReplacementValue(field, originalValues[field]);
                }

                basicInfoPage.SetFieldValue(field, replacement);
                var inEditValue = basicInfoPage.GetFieldValue(field);
                AssertValuesEqual(field, replacement, inEditValue, $"Field '{field}' did not reflect the edited value while in edit mode.");
            }

            var caseHomeAfterCancel = basicInfoPage.CancelChanges();
            Assert.True(caseHomeAfterCancel.IsLoaded, "Case home page did not load after cancelling edit information.");

            var summaryAfterCancel = caseHomeAfterCancel.GetBasicInformationSummary();
            AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.AlternateId, summaryBefore.AlternateId, summaryAfterCancel.AlternateId,
                "Alternate ID summary value changed after cancelling edits.");
            AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.ScreenDate, summaryBefore.ScreenDate, summaryAfterCancel.ScreenDate,
                "Screen Date summary value changed after cancelling edits.");
            AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob, summaryBefore.TargetChildDob, summaryAfterCancel.TargetChildDob,
                "Target Child DOB summary value changed after cancelling edits.");
            AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.IntakeDate, summaryBefore.IntakeDate, summaryAfterCancel.IntakeDate,
                "Intake Date summary value changed after cancelling edits.");
            AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate, summaryBefore.ParentSurveyDate, summaryAfterCancel.ParentSurveyDate,
                "Parent Survey Date summary value changed after cancelling edits.");

            var basicInfoAfterCancel = caseHomeAfterCancel.OpenBasicInformationEditor();
            foreach (var field in basicInfoAfterCancel.EditableFields)
            {
                var valueAfterCancel = basicInfoAfterCancel.GetFieldValue(field);
                AssertValuesEqual(field, originalValues[field], valueAfterCancel, $"Field '{field}' did not revert to its original value after cancelling.");
            }
        }

        private CaseHomePage NavigateToCaseHome(IPookieWebDriver driver)
        {
            var routine = new SearchCasesSearchRoutine(driver, _config);
            var parameters = SearchCasesSearchRoutine.GetParameters();

            parameters.Criteria = new SearchCasesCriteria
            {
                Pc1Id = KnownPc1Id,
                Pc1FirstName = KnownPc1FirstName,
                Pc1LastName = KnownPc1LastName,
                TcDob = KnownTcDob,
                WorkerDisplayText = KnownWorkerDisplayText,
                AlternateId = KnownAlternateId
            };

            routine.LoadApplication(parameters);
            routine.EnsureSignedIn(parameters);
            routine.EnsureRoleSelected(parameters);
            routine.NavigateToSearchCases(parameters);
            routine.PopulateSearchCriteria(parameters);
            routine.SubmitSearch(parameters);

            Assert.True(parameters.SignedIn, "User was not signed in while navigating to Basic Case Information.");
            Assert.True(parameters.RoleSelected, "Role selection did not complete successfully while navigating to Basic Case Information.");
            Assert.True(parameters.SearchCasesPageLoaded, "Search Cases page did not load when preparing to view Basic Case Information.");
            Assert.True(parameters.SearchCompleted, "Case search did not complete when preparing to view Basic Case Information.");

            var firstResult = parameters.FirstResult
                             ?? throw new InvalidOperationException("No search results were returned while navigating to Basic Case Information.");

            var caseHomePage = firstResult.OpenCaseHome();

            Assert.NotNull(caseHomePage);
            Assert.True(caseHomePage.IsLoaded, "Case Home page did not load after opening a case from search results.");

            return caseHomePage;
        }

        private static string GenerateReplacementValue(BasicCaseInformationPage.BasicCaseInformationField field, string originalValue)
        {
            return field switch
            {
                BasicCaseInformationPage.BasicCaseInformationField.AlternateId =>
                    string.IsNullOrWhiteSpace(originalValue)
                        ? "AutoAlternate-Test"
                        : $"{originalValue.Trim()}-Test",

                BasicCaseInformationPage.BasicCaseInformationField.ScreenDate => GenerateDateReplacement(originalValue, 1),
                BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob => GenerateDateReplacement(originalValue, 1),
                BasicCaseInformationPage.BasicCaseInformationField.IntakeDate => GenerateDateReplacement(originalValue, 1),
                BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate => GenerateDateReplacement(originalValue, 1),

                _ => string.IsNullOrWhiteSpace(originalValue)
                        ? "AutoValue-Test"
                        : $"{originalValue.Trim()}-Test"
            };
        }

        private static string GenerateAlternateReplacementValue(BasicCaseInformationPage.BasicCaseInformationField field, string originalValue)
        {
            return field switch
            {
                BasicCaseInformationPage.BasicCaseInformationField.AlternateId =>
                    string.IsNullOrWhiteSpace(originalValue)
                        ? "AutoAlternate-Verify"
                        : $"{originalValue.Trim()}-Verify",

                BasicCaseInformationPage.BasicCaseInformationField.ScreenDate => GenerateDateReplacement(originalValue, 2),
                BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob => GenerateDateReplacement(originalValue, 2),
                BasicCaseInformationPage.BasicCaseInformationField.IntakeDate => GenerateDateReplacement(originalValue, 2),
                BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate => GenerateDateReplacement(originalValue, 2),

                _ => string.IsNullOrWhiteSpace(originalValue)
                        ? "AutoValue-Verify"
                        : $"{originalValue.Trim()}-Verify"
            };
        }

        private static bool ValuesAreEquivalent(BasicCaseInformationPage.BasicCaseInformationField field, string expected, string actual)
        {
            if (IsDateField(field))
            {
                return AreDateValuesEquivalent(expected, actual);
            }

            return string.Equals(expected?.Trim(), actual?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertValuesEqual(BasicCaseInformationPage.BasicCaseInformationField field, string expected, string actual, string message)
        {
            if (IsDateField(field))
            {
                Assert.True(AreDateValuesEquivalent(expected, actual), message);
                return;
            }

            Assert.Equal(expected?.Trim(), actual?.Trim());
        }

        private static bool IsDateField(BasicCaseInformationPage.BasicCaseInformationField field)
        {
            return field is BasicCaseInformationPage.BasicCaseInformationField.ScreenDate
                        or BasicCaseInformationPage.BasicCaseInformationField.TargetChildDob
                        or BasicCaseInformationPage.BasicCaseInformationField.IntakeDate
                        or BasicCaseInformationPage.BasicCaseInformationField.ParentSurveyDate;
        }

        private static bool AreDateValuesEquivalent(string expected, string actual)
        {
            if (!TryParseDate(expected, out var expectedDate) || !TryParseDate(actual, out var actualDate))
            {
                return string.Equals(expected?.Trim(), actual?.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            return expectedDate.Date == actualDate.Date;
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                date = default;
                return false;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static string GenerateDateReplacement(string originalValue, int daysToAdd)
        {
            if (TryParseDate(originalValue, out var originalDate))
            {
                var candidate = originalDate.AddDays(daysToAdd);
                if (candidate.Date == originalDate.Date)
                {
                    candidate = originalDate.AddDays(daysToAdd + 1);
                }

                return candidate.ToString("MM/dd/yy", CultureInfo.InvariantCulture);
            }

            var baseline = DateTime.Today.AddDays(Math.Max(daysToAdd, 1));
            return baseline.ToString("MM/dd/yy", CultureInfo.InvariantCulture);
        }
    }
}

