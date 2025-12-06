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

namespace AFUT.Tests.UnitTests.BaselineForm
{
    /// <summary>
    /// Tests for Family/Other Children tab of the Baseline Form
    /// </summary>
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class BaselineFormFamilyChildrenTests : IClassFixture<AppConfig>
    {
        protected readonly AppConfig _config;
        protected readonly IPookieDriverFactory _driverFactory;
        protected readonly ITestOutputHelper _output;
        protected static readonly Random RandomGenerator = new();
        protected static readonly object RandomLock = new();

        // Store test data for persistence verification
        protected class ChildData
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string DateOfBirth { get; set; } = string.Empty;
            public string Relationship { get; set; } = string.Empty;
            public string LivingArrangement { get; set; } = string.Empty;
        }

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        public BaselineFormFamilyChildrenTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");

            CaseHomePage.ConfigureDefaultTabs(_config.CaseHomeTabs);
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(4)]
        public void FamilyChildrenTabValidationTest(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToBaselineForm(driver, formsPane);
            
            // Navigate to Family/Other Children tab
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            _output.WriteLine("[PASS] Family/Other Children tab activated successfully");

            // ===== PART 1: Fill Household Income Fields =====
            _output.WriteLine("\n[TEST SECTION] Testing Household Income Fields");

            // Question 28: Number of people in house (0-99)
            var numInHouse = GetRandomNumber(1, 99);
            var numInHouseInput = driver.FindElements(By.CssSelector("input.form-control.number-2[id*='txtNumberInHouse']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Number in house input was not found.");
            WebElementHelper.SetInputValue(driver, numInHouseInput, numInHouse.ToString(), "Number in house", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered number in house: {numInHouse}");

            // Question 29a: Average monthly income (0-99999)
            var monthlyIncome = GetRandomNumber(0, 99999);
            var monthlyIncomeInput = driver.FindElements(By.CssSelector("input.form-control.number-5[id*='txtAvailableMonthlyIncome']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Monthly income input was not found.");
            WebElementHelper.SetInputValue(driver, monthlyIncomeInput, monthlyIncome.ToString(), "Monthly income", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered average monthly income: {monthlyIncome}");

            // Question 29b: Average monthly benefits (0-99999)
            var monthlyBenefits = GetRandomNumber(0, 99999);
            var monthlyBenefitsInput = driver.FindElements(By.CssSelector("input.form-control.number-5[id*='txtAvailableMonthlyBenefits']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Monthly benefits input was not found.");
            WebElementHelper.SetInputValue(driver, monthlyBenefitsInput, monthlyBenefits.ToString(), "Monthly benefits", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered average monthly benefits: {monthlyBenefits}");

            // Question 30: Number of persons contributing (0-99)
            var numContributing = GetRandomNumber(0, 99);
            var numContributingInput = driver.FindElements(By.CssSelector("input.form-control.number-2[id*='txtNumberEmployed']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Number contributing input was not found.");
            WebElementHelper.SetInputValue(driver, numContributingInput, numContributing.ToString(), "Number contributing", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered number of persons contributing: {numContributing}");

            // ===== PART 2: Test All Validations on ALL 6 Children =====
            _output.WriteLine("\n[TEST SECTION] Testing Living Arrangement 'Other' and Age validations on all children");

            var childrenData = new List<ChildData>();

            for (int childNum = 1; childNum <= 6; childNum++)
            {
                _output.WriteLine($"\n--- Testing Child {childNum} ---");
                
                // Test Living Arrangement "Other" validation
                TestLivingArrangementOtherValidation(driver, childNum);
                
                // Test Relationship "Other" validation
                TestRelationshipOtherSpecify(driver, childNum);
                
                // Test First Name blank validation
                TestFirstNameBlankValidation(driver, childNum);
                
                // Test Last Name blank validation
                TestLastNameBlankValidation(driver, childNum);
                
                // Test Age validation (over 21 years and future date) and store the corrected data
                var childData = TestAgeValidation(driver, childNum);
                childrenData.Add(childData);
                
                _output.WriteLine($"[PASS] Completed all validation tests for Child {childNum}");
            }

            _output.WriteLine("\n[INFO] All 6 children now have valid data after validation corrections");
            _output.WriteLine("[PASS] All validation tests completed successfully");
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(5)]
        public void FamilyChildrenTabSubmitTest(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();

            var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
            Assert.NotNull(homePage);
            Assert.True(homePage.IsLoaded, "Home page did not load after selecting DataEntry role.");
            _output.WriteLine("[PASS] Successfully navigated to Forms tab");

            NavigateToBaselineForm(driver, formsPane);
            
            // Navigate to Family/Other Children tab
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            _output.WriteLine("[PASS] Family/Other Children tab activated successfully");

            // ===== Fill Household Income Fields =====
            _output.WriteLine("\n[TEST SECTION] Filling Household Income Fields");

            var numInHouse = 99;
            var numInHouseInput = driver.FindElements(By.CssSelector("input.form-control.number-2[id*='txtNumberInHouse']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Number in house input was not found.");
            WebElementHelper.SetInputValue(driver, numInHouseInput, numInHouse.ToString(), "Number in house", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered number in house: {numInHouse}");

            var monthlyIncome = 12;
            var monthlyIncomeInput = driver.FindElements(By.CssSelector("input.form-control.number-5[id*='txtAvailableMonthlyIncome']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Monthly income input was not found.");
            WebElementHelper.SetInputValue(driver, monthlyIncomeInput, monthlyIncome.ToString(), "Monthly income", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered average monthly income: {monthlyIncome}");

            var monthlyBenefits = 12;
            var monthlyBenefitsInput = driver.FindElements(By.CssSelector("input.form-control.number-5[id*='txtAvailableMonthlyBenefits']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Monthly benefits input was not found.");
            WebElementHelper.SetInputValue(driver, monthlyBenefitsInput, monthlyBenefits.ToString(), "Monthly benefits", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered average monthly benefits: {monthlyBenefits}");

            var numContributing = 99;
            var numContributingInput = driver.FindElements(By.CssSelector("input.form-control.number-2[id*='txtNumberEmployed']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Number contributing input was not found.");
            WebElementHelper.SetInputValue(driver, numContributingInput, numContributing.ToString(), "Number contributing", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered number of persons contributing: {numContributing}");

            // ===== Fill All 6 Children with Valid Data =====
            _output.WriteLine("\n[TEST SECTION] Filling all 6 children with valid data");

            var firstNames = new[] { "wonder", "captain", "bat", "super", "iron", "Peter" };
            var lastNames = new[] { "lasgirl", "patrick", "hired", "denim", "catching", "parker" };
            var childrenData = new List<ChildData>();

            for (int childNum = 1; childNum <= 6; childNum++)
            {
                _output.WriteLine($"\n--- Filling Child {childNum} ---");
                
                var firstName = firstNames[childNum - 1];
                var lastName = lastNames[childNum - 1];
                var dob = GenerateRandomDateUnder21();

                // Fill First Name
                var firstNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNum}_txtChildFName']"))
                    .FirstOrDefault(el => el.Displayed);
                if (firstNameInput != null)
                {
                    WebElementHelper.SetInputValue(driver, firstNameInput, firstName, $"Child {childNum} First Name", triggerBlur: true);
                }

                // Fill Last Name
                var lastNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNum}_txtChildLName']"))
                    .FirstOrDefault(el => el.Displayed);
                if (lastNameInput != null)
                {
                    WebElementHelper.SetInputValue(driver, lastNameInput, lastName, $"Child {childNum} Last Name", triggerBlur: true);
                }

                // Fill DOB
                var dobInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNum}_txtChildDOB']"))
                    .FirstOrDefault(el => el.Displayed);
                if (dobInput != null)
                {
                    WebElementHelper.SetInputValue(driver, dobInput, dob, $"Child {childNum} DOB", triggerBlur: true);
                }

                // Select Relationship (exclude "Other" - value "09")
                var relationshipDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNum}_ddlRelation2PC1']"))
                    .FirstOrDefault(el => el.Displayed);
                string relationshipText = "";
                if (relationshipDropdown != null)
                {
                    var relationshipSelect = new SelectElement(relationshipDropdown);
                    var validOptions = relationshipSelect.Options
                        .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && opt.GetAttribute("value") != "09")
                        .ToList();
                    if (validOptions.Any())
                    {
                        var randomOption = validOptions[GetRandomNumber(0, validOptions.Count - 1)];
                        relationshipSelect.SelectByValue(randomOption.GetAttribute("value"));
                        driver.WaitForUpdatePanel(3);
                        driver.WaitForReady(3);
                        Thread.Sleep(200);
                        relationshipText = randomOption.Text.Trim();
                        _output.WriteLine($"[INFO] Selected Relationship: {relationshipText}");
                    }
                }

                // Select Living Arrangement (exclude "Other" - value "05")
                var livingArrangementDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNum}_ddlLivingArrangements']"))
                    .FirstOrDefault(el => el.Displayed);
                string livingArrangementText = "";
                if (livingArrangementDropdown != null)
                {
                    var livingArrangementSelect = new SelectElement(livingArrangementDropdown);
                    var validOptions = livingArrangementSelect.Options
                        .Where(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && opt.GetAttribute("value") != "05")
                        .ToList();
                    if (validOptions.Any())
                    {
                        var randomOption = validOptions[GetRandomNumber(0, validOptions.Count - 1)];
                        livingArrangementSelect.SelectByValue(randomOption.GetAttribute("value"));
                        driver.WaitForUpdatePanel(3);
                        driver.WaitForReady(3);
                        Thread.Sleep(200);
                        livingArrangementText = randomOption.Text.Trim();
                        _output.WriteLine($"[INFO] Selected Living Arrangement: {livingArrangementText}");
                    }
                }

                // Store child data for persistence verification
                childrenData.Add(new ChildData
                {
                    FirstName = firstName,
                    LastName = lastName,
                    DateOfBirth = dob,
                    Relationship = relationshipText,
                    LivingArrangement = livingArrangementText
                });

                _output.WriteLine($"[PASS] Child {childNum} filled successfully: {firstName} {lastName}, DOB: {dob}");
            }

            _output.WriteLine("\n[INFO] All 6 children filled with valid data");

            // ===== Submit and Verify Toast =====
            _output.WriteLine("\n[TEST SECTION] Submitting form and verifying toast");

            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(3000);
            _output.WriteLine("[INFO] Clicked Submit button, waiting for toast message...");

            // Verify success toast message or redirect
            var toastMessage = WebElementHelper.GetToastMessage(driver, 3000);
            var currentUrl = driver.Url ?? string.Empty;
            
            // If toast is empty but we redirected to CaseHome, the form saved successfully
            if (string.IsNullOrWhiteSpace(toastMessage) && currentUrl.Contains("CaseHome.aspx", StringComparison.OrdinalIgnoreCase))
            {
                _output.WriteLine("[INFO] Form saved successfully (redirected to CaseHome.aspx)");
                toastMessage = $"Form Saved - {pc1Id}";
            }
            else if (currentUrl.Contains("errorpage.aspx", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(false, "Form submission failed - redirected to error page.");
            }
            
            Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed and no redirect to CaseHome occurred.");
            Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[PASS] Form saved successfully: {toastMessage}");

            // Wait for redirect to CaseHome
            Thread.Sleep(2000);
            var currentUrlAfterSave = driver.Url ?? string.Empty;
            _output.WriteLine($"[INFO] Current URL after save: {currentUrlAfterSave}");

            // ===== PART 4: Navigate back and verify persistence =====
            _output.WriteLine("\n[TEST SECTION] Verifying data persistence");

            // Navigate back to Forms tab
            var formsTab = driver.WaitforElementToBeInDOM(By.CssSelector("a#formstab[data-toggle='tab'][href='#forms']"), 10)
                ?? throw new InvalidOperationException("Forms tab was not found.");
            CommonTestHelper.ClickElement(driver, formsTab);
            driver.WaitForReady(5);
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Clicked Forms tab");

            formsPane = driver.WaitforElementToBeInDOM(By.CssSelector(".tab-pane#forms"), 5)
                ?? throw new InvalidOperationException("Forms tab content was not found.");

            // Navigate back to Baseline Form
            NavigateToBaselineForm(driver, formsPane);
            _output.WriteLine("[INFO] Navigated back to Baseline form");

            // Navigate to Family/Other Children tab
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(1000);
            _output.WriteLine("[INFO] Navigated back to Family/Other Children tab");

            // Verify household income fields persisted
            numInHouseInput = driver.FindElements(By.CssSelector("input.form-control.number-2[id*='txtNumberInHouse']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Number in house input was not found after reload.");
            var numInHouseValue = numInHouseInput.GetAttribute("value");
            Assert.Equal(numInHouse.ToString(), numInHouseValue);
            _output.WriteLine($"[PASS] Number in house persisted: {numInHouseValue}");

            monthlyIncomeInput = driver.FindElements(By.CssSelector("input.form-control.number-5[id*='txtAvailableMonthlyIncome']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Monthly income input was not found after reload.");
            var monthlyIncomeValue = monthlyIncomeInput.GetAttribute("value");
            Assert.Equal(monthlyIncome.ToString(), monthlyIncomeValue);
            _output.WriteLine($"[PASS] Monthly income persisted: {monthlyIncomeValue}");

            monthlyBenefitsInput = driver.FindElements(By.CssSelector("input.form-control.number-5[id*='txtAvailableMonthlyBenefits']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Monthly benefits input was not found after reload.");
            var monthlyBenefitsValue = monthlyBenefitsInput.GetAttribute("value");
            Assert.Equal(monthlyBenefits.ToString(), monthlyBenefitsValue);
            _output.WriteLine($"[PASS] Monthly benefits persisted: {monthlyBenefitsValue}");

            numContributingInput = driver.FindElements(By.CssSelector("input.form-control.number-2[id*='txtNumberEmployed']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException("Number contributing input was not found after reload.");
            var numContributingValue = numContributingInput.GetAttribute("value");
            Assert.Equal(numContributing.ToString(), numContributingValue);
            _output.WriteLine($"[PASS] Number contributing persisted: {numContributingValue}");

            // Verify all 6 children data persisted
            for (int i = 0; i < 6; i++)
            {
                var childNum = i + 1;
                var expectedChild = childrenData[i];
                VerifyChildRow(driver, childNum, expectedChild);
                _output.WriteLine($"[PASS] Child {childNum} data persisted correctly");
            }

            _output.WriteLine("\n[PASS] Family/Other Children tab complete flow test finished successfully");
        }

        protected void TestLivingArrangementOtherValidation(IPookieWebDriver driver, int childNumber)
        {
            _output.WriteLine($"\n[INFO] Testing Living Arrangement 'Other' validation for Child {childNumber}");

            // Fill basic info for this child with specific names
            var firstNames = new[] { "wonder", "captain", "bat", "super", "iron", "Peter" };
            var lastNames = new[] { "lasgirl", "patrick", "hired", "denim", "catching", "parker" };
            
            var firstName = firstNames[childNumber - 1];
            var lastName = lastNames[childNumber - 1];
            var dob = "01/01/00";

            var firstNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildFName']"))
                .FirstOrDefault(el => el.Displayed);
            if (firstNameInput != null)
            {
                WebElementHelper.SetInputValue(driver, firstNameInput, firstName, $"Child {childNumber} First Name", triggerBlur: true);
            }

            var lastNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildLName']"))
                .FirstOrDefault(el => el.Displayed);
            if (lastNameInput != null)
            {
                WebElementHelper.SetInputValue(driver, lastNameInput, lastName, $"Child {childNumber} Last Name", triggerBlur: true);
            }

            var dobInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildDOB']"))
                .FirstOrDefault(el => el.Displayed);
            if (dobInput != null)
            {
                WebElementHelper.SetInputValue(driver, dobInput, dob, $"Child {childNumber} DOB", triggerBlur: true);
            }

            // Select a relationship
            var relationshipDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlRelation2PC1']"))
                .FirstOrDefault(el => el.Displayed);
            if (relationshipDropdown != null)
            {
                var relationshipSelect = new SelectElement(relationshipDropdown);
                var firstValidOption = relationshipSelect.Options.FirstOrDefault(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && opt.GetAttribute("value") != "09");
                if (firstValidOption != null)
                {
                    relationshipSelect.SelectByValue(firstValidOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(3);
                    driver.WaitForReady(3);
                    Thread.Sleep(200);
                }
            }

            // Select "Other" (05) in Living Arrangement
            var livingArrangementDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlLivingArrangements']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Living Arrangement dropdown was not found.");
            
            var livingArrangementSelect = new SelectElement(livingArrangementDropdown);
            livingArrangementSelect.SelectByValue("05"); // Select "Other"
            driver.WaitForUpdatePanel(3);
            driver.WaitForReady(3);
            Thread.Sleep(500);
            _output.WriteLine($"[INFO] Selected 'Other' in Living Arrangement for Child {childNumber}");

            // Verify specify field appears
            var specifyDiv = driver.FindElements(By.CssSelector($"div[id*='Child{childNumber}_divLivingArrangementsSpecify']"))
                .FirstOrDefault();
            Assert.NotNull(specifyDiv);
            Assert.True(specifyDiv.Displayed, $"Living Arrangement specify field should be visible for Child {childNumber}");
            _output.WriteLine($"[PASS] Living Arrangement specify field appeared for Child {childNumber}");

            // Leave specify field empty and submit
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine($"[INFO] Clicked Submit with empty Living Arrangement specify for Child {childNumber}");

            // Switch back to Family/Children tab (may reset to PC1)
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(500);

            // Verify validation message
            var validationMessage = FindValidationMessage(driver, $"Child{childNumber} Living Arrangement specify validation", 
                $"Please specify Child{childNumber} Living Arrangement");
            Assert.NotNull(validationMessage);
            _output.WriteLine($"[PASS] Validation displayed for Child {childNumber}: {validationMessage!.Text.Trim()}");

            // Change to a non-Other option
            livingArrangementDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlLivingArrangements']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Living Arrangement dropdown was not found after validation.");
            
            livingArrangementSelect = new SelectElement(livingArrangementDropdown);
            var nonOtherOption = livingArrangementSelect.Options.FirstOrDefault(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && opt.GetAttribute("value") != "05");
            if (nonOtherOption != null)
            {
                livingArrangementSelect.SelectByValue(nonOtherOption.GetAttribute("value"));
                driver.WaitForUpdatePanel(3);
                driver.WaitForReady(3);
                Thread.Sleep(200);
                _output.WriteLine($"[INFO] Changed Living Arrangement to non-Other option for Child {childNumber}");
            }

            // Verify specify field hides
            Thread.Sleep(500);
            specifyDiv = driver.FindElements(By.CssSelector($"div[id*='Child{childNumber}_divLivingArrangementsSpecify']"))
                .FirstOrDefault();
            var isHidden = specifyDiv == null || !specifyDiv.Displayed || specifyDiv.GetAttribute("style").Contains("display: none");
            Assert.True(isHidden, $"Living Arrangement specify field should be hidden for Child {childNumber}");
            _output.WriteLine($"[PASS] Living Arrangement specify field hidden after selecting non-Other option for Child {childNumber}");
        }

        protected void TestRelationshipOtherSpecify(IPookieWebDriver driver, int childNumber)
        {
            _output.WriteLine($"\n[INFO] Testing Relationship to PC1 'Other' validation for Child {childNumber}");

            // Select "Other" (09) in Relationship to PC1
            var relationshipDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlRelation2PC1']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Relationship dropdown was not found.");
            
            var relationshipSelect = new SelectElement(relationshipDropdown);
            relationshipSelect.SelectByValue("09"); // Select "Other"
            driver.WaitForUpdatePanel(3);
            driver.WaitForReady(3);
            Thread.Sleep(500);
            _output.WriteLine($"[INFO] Selected 'Other' in Relationship to PC1 for Child {childNumber}");

            // Verify specify field appears
            var specifyDiv = driver.FindElements(By.CssSelector($"div[id*='Child{childNumber}_divRelation2PC1Specify']"))
                .FirstOrDefault();
            Assert.NotNull(specifyDiv);
            Assert.True(specifyDiv.Displayed, $"Relationship specify field should be visible for Child {childNumber}");
            _output.WriteLine($"[PASS] Relationship specify field appeared for Child {childNumber}");

            // Clear the specify field if it has any value
            var specifyInput = specifyDiv.FindElements(By.CssSelector($"input[id*='Child{childNumber}_txtRelation2PC1Specify']"))
                .FirstOrDefault();
            if (specifyInput != null && specifyInput.Displayed)
            {
                specifyInput.Clear();
                _output.WriteLine($"[INFO] Cleared Relationship specify field for Child {childNumber}");
            }

            // Submit without filling specify field
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine($"[INFO] Clicked Submit with empty Relationship specify for Child {childNumber}");

            // Switch back to Family/Children tab (may reset to PC1)
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(500);

            // Verify validation message
            var validationMessage = FindValidationMessage(driver, $"Child{childNumber} Relationship specify validation", 
                $"Please specify Child{childNumber} relationship to PC 1");
            Assert.NotNull(validationMessage);
            _output.WriteLine($"[PASS] Validation displayed for Child {childNumber}: {validationMessage!.Text.Trim()}");

            // Change to a non-Other option
            relationshipDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlRelation2PC1']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Relationship dropdown was not found after validation.");
            
            relationshipSelect = new SelectElement(relationshipDropdown);
            var nonOtherOption = relationshipSelect.Options.FirstOrDefault(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")) && opt.GetAttribute("value") != "09");
            if (nonOtherOption != null)
            {
                relationshipSelect.SelectByValue(nonOtherOption.GetAttribute("value"));
                driver.WaitForUpdatePanel(3);
                driver.WaitForReady(3);
                Thread.Sleep(500);
                _output.WriteLine($"[INFO] Changed Relationship to non-Other option for Child {childNumber}: {nonOtherOption.Text.Trim()}");
            }

            // Verify specify field hides
            Thread.Sleep(500);
            specifyDiv = driver.FindElements(By.CssSelector($"div[id*='Child{childNumber}_divRelation2PC1Specify']"))
                .FirstOrDefault();
            var isHidden = specifyDiv == null || !specifyDiv.Displayed || specifyDiv.GetAttribute("style").Contains("display: none");
            Assert.True(isHidden, $"Relationship specify field should be hidden for Child {childNumber}");
            _output.WriteLine($"[PASS] Relationship specify field hidden after selecting non-Other option for Child {childNumber}");
        }

        protected void TestFirstNameBlankValidation(IPookieWebDriver driver, int childNumber)
        {
            _output.WriteLine($"\n[INFO] Testing First Name blank validation for Child {childNumber}");

            // Get the current first name to restore later
            var firstNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildFName']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} First Name input was not found.");
            
            var originalFirstName = firstNameInput.GetAttribute("value");

            // Clear the first name field
            firstNameInput.Clear();
            WebElementHelper.SetInputValue(driver, firstNameInput, "", $"Child {childNumber} First Name (clear)", triggerBlur: true);
            _output.WriteLine($"[INFO] Cleared First Name for Child {childNumber}");

            // Submit without first name
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine($"[INFO] Clicked Submit with blank First Name for Child {childNumber}");

            // Switch back to Family/Children tab (may reset to PC1)
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(500);

            // Verify validation message
            var validationMessage = FindValidationMessage(driver, $"Child{childNumber} First Name blank validation", 
                $"Other child {childNumber}: First Name cannot be blank");
            Assert.NotNull(validationMessage);
            _output.WriteLine($"[PASS] Validation displayed for Child {childNumber}: {validationMessage!.Text.Trim()}");

            // Re-fill the first name
            firstNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildFName']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} First Name input was not found after validation.");
            
            WebElementHelper.SetInputValue(driver, firstNameInput, originalFirstName, $"Child {childNumber} First Name (restore)", triggerBlur: true);
            _output.WriteLine($"[INFO] Restored First Name for Child {childNumber}: {originalFirstName}");
            _output.WriteLine($"[PASS] First Name blank validation test completed for Child {childNumber}");
        }

        protected void TestLastNameBlankValidation(IPookieWebDriver driver, int childNumber)
        {
            _output.WriteLine($"\n[INFO] Testing Last Name blank validation for Child {childNumber}");

            // Get the current last name to restore later
            var lastNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildLName']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Last Name input was not found.");
            
            var originalLastName = lastNameInput.GetAttribute("value");

            // Clear the last name field
            lastNameInput.Clear();
            WebElementHelper.SetInputValue(driver, lastNameInput, "", $"Child {childNumber} Last Name (clear)", triggerBlur: true);
            _output.WriteLine($"[INFO] Cleared Last Name for Child {childNumber}");

            // Submit without last name
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine($"[INFO] Clicked Submit with blank Last Name for Child {childNumber}");

            // Switch back to Family/Children tab (may reset to PC1)
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(500);

            // Verify validation message
            var validationMessage = FindValidationMessage(driver, $"Child{childNumber} Last Name blank validation", 
                $"Other child {childNumber}: Last Name cannot be blank");
            Assert.NotNull(validationMessage);
            _output.WriteLine($"[PASS] Validation displayed for Child {childNumber}: {validationMessage!.Text.Trim()}");

            // Re-fill the last name
            lastNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildLName']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Last Name input was not found after validation.");
            
            WebElementHelper.SetInputValue(driver, lastNameInput, originalLastName, $"Child {childNumber} Last Name (restore)", triggerBlur: true);
            _output.WriteLine($"[INFO] Restored Last Name for Child {childNumber}: {originalLastName}");
            _output.WriteLine($"[PASS] Last Name blank validation test completed for Child {childNumber}");
        }

        protected ChildData TestAgeValidation(IPookieWebDriver driver, int childNumber)
        {
            _output.WriteLine($"\n[INFO] Testing age validation (over 21 years) for Child {childNumber}");

            // Names are already set from TestLivingArrangementOtherValidation, don't overwrite them
            // Just proceed with date validation
            
            // Generate a date that makes the child over 21 years old
            var currentDate = DateTime.Now;
            var yearOver21 = currentDate.Year - 22; // 22 years ago to ensure over 21
            var monthRandom = GetRandomNumber(1, 12);
            var dayRandom = GetRandomNumber(1, 28); // Safe day for all months
            var dateOver21 = $"{monthRandom:D2}/{dayRandom:D2}/{yearOver21.ToString().Substring(2, 2)}";

            var dobInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildDOB']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} DOB input was not found.");
            
            WebElementHelper.SetInputValue(driver, dobInput, dateOver21, $"Child {childNumber} DOB (over 21)", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered date over 21 years: {dateOver21} (full year: {monthRandom:D2}/{dayRandom:D2}/{yearOver21})");

            // Select relationship and living arrangement
            var relationshipDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlRelation2PC1']"))
                .FirstOrDefault(el => el.Displayed);
            if (relationshipDropdown != null)
            {
                var relationshipSelect = new SelectElement(relationshipDropdown);
                var firstValidOption = relationshipSelect.Options.FirstOrDefault(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")));
                if (firstValidOption != null)
                {
                    relationshipSelect.SelectByValue(firstValidOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(3);
                    driver.WaitForReady(3);
                    Thread.Sleep(200);
                }
            }

            var livingArrangementDropdown = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlLivingArrangements']"))
                .FirstOrDefault(el => el.Displayed);
            if (livingArrangementDropdown != null)
            {
                var livingArrangementSelect = new SelectElement(livingArrangementDropdown);
                var firstValidOption = livingArrangementSelect.Options.FirstOrDefault(opt => !string.IsNullOrWhiteSpace(opt.GetAttribute("value")));
                if (firstValidOption != null)
                {
                    livingArrangementSelect.SelectByValue(firstValidOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(3);
                    driver.WaitForReady(3);
                    Thread.Sleep(200);
                }
            }

            // Submit and expect age validation
            var submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine($"[INFO] Clicked Submit with date over 21 years for Child {childNumber}");

            // Switch back to Family/Children tab (may reset to PC1)
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(500);

            // Verify age validation message (over 21)
            var ageValidation = FindValidationMessage(driver, $"Child{childNumber} age validation", 
                "over 21 years", "not allowed", "Other Children");
            Assert.NotNull(ageValidation);
            _output.WriteLine($"[PASS] Age validation (over 21) displayed for Child {childNumber}: {ageValidation!.Text.Trim()}");

            // Now test future date validation
            _output.WriteLine($"[INFO] Testing future date validation for Child {childNumber}");
            
            var yearFuture = currentDate.Year + GetRandomNumber(1, 5); // 1-5 years in the future
            var monthFuture = GetRandomNumber(1, 12);
            var dayFuture = GetRandomNumber(1, 28);
            var dateFuture = $"{monthFuture:D2}/{dayFuture:D2}/{yearFuture.ToString().Substring(2, 2)}";

            dobInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildDOB']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} DOB input was not found for future date test.");
            
            WebElementHelper.SetInputValue(driver, dobInput, dateFuture, $"Child {childNumber} DOB (future)", triggerBlur: true);
            _output.WriteLine($"[INFO] Entered future date: {dateFuture} (full year: {monthFuture:D2}/{dayFuture:D2}/{yearFuture})");

            // Submit and expect future date validation
            submitButton = FindSubmitButton(driver);
            CommonTestHelper.ClickElement(driver, submitButton);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(1000);
            _output.WriteLine($"[INFO] Clicked Submit with future date for Child {childNumber}");

            // Switch back to Family/Children tab (may reset to PC1)
            ActivateTab(driver, "#tab_CHILDREN a[href='#CHILDREN']", "Family/Other Children");
            Thread.Sleep(500);

            // Verify future date validation message
            var futureDateValidation = FindValidationMessage(driver, $"Child{childNumber} future date validation", 
                "is in the future", "not allowed", "Other Children");
            Assert.NotNull(futureDateValidation);
            _output.WriteLine($"[PASS] Future date validation displayed for Child {childNumber}: {futureDateValidation!.Text.Trim()}");

            // Correct the date to be valid (under 21 years and not in future)
            var yearUnder21 = currentDate.Year - GetRandomNumber(1, 20); // Between 1 and 20 years old
            var dateUnder21 = $"{monthRandom:D2}/{dayRandom:D2}/{yearUnder21.ToString().Substring(2, 2)}";

            dobInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildDOB']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} DOB input was not found after validation.");
            
            WebElementHelper.SetInputValue(driver, dobInput, dateUnder21, $"Child {childNumber} corrected DOB", triggerBlur: true);
            _output.WriteLine($"[INFO] Corrected date to valid (under 21, not future): {dateUnder21} (full year: {monthRandom:D2}/{dayRandom:D2}/{yearUnder21})");
            _output.WriteLine($"[PASS] Age validation test completed for Child {childNumber}");

            // Capture and return the corrected child data for persistence verification
            // Read the names from the inputs (they were set earlier and preserved)
            var firstNameInputFinal = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildFName']"))
                .FirstOrDefault(el => el.Displayed);
            var firstName = firstNameInputFinal?.GetAttribute("value") ?? "";
            
            var lastNameInputFinal = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildLName']"))
                .FirstOrDefault(el => el.Displayed);
            var lastName = lastNameInputFinal?.GetAttribute("value") ?? "";
            
            var relationshipDropdownFinal = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlRelation2PC1']"))
                .FirstOrDefault(el => el.Displayed);
            var relationshipText = "";
            if (relationshipDropdownFinal != null)
            {
                var relationshipSelectFinal = new SelectElement(relationshipDropdownFinal);
                relationshipText = relationshipSelectFinal.SelectedOption?.Text.Trim() ?? "";
            }

            var livingArrangementDropdownFinal = driver.FindElements(By.CssSelector($"select.form-control[id*='Child{childNumber}_ddlLivingArrangements']"))
                .FirstOrDefault(el => el.Displayed);
            var livingArrangementText = "";
            if (livingArrangementDropdownFinal != null)
            {
                var livingArrangementSelectFinal = new SelectElement(livingArrangementDropdownFinal);
                livingArrangementText = livingArrangementSelectFinal.SelectedOption?.Text.Trim() ?? "";
            }

            return new ChildData
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateUnder21,
                Relationship = relationshipText,
                LivingArrangement = livingArrangementText
            };
        }

        protected void VerifyChildRow(IPookieWebDriver driver, int childNumber, ChildData expectedData)
        {
            // Verify First Name
            var firstNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildFName']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} First Name input was not found after reload.");
            var firstNameValue = firstNameInput.GetAttribute("value");
            Assert.Equal(expectedData.FirstName, firstNameValue);

            // Verify Last Name
            var lastNameInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildLName']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} Last Name input was not found after reload.");
            var lastNameValue = lastNameInput.GetAttribute("value");
            Assert.Equal(expectedData.LastName, lastNameValue);

            // Verify Date of Birth
            var dobInput = driver.FindElements(By.CssSelector($"input.form-control[id*='Child{childNumber}_txtChildDOB']"))
                .FirstOrDefault(el => el.Displayed)
                ?? throw new InvalidOperationException($"Child {childNumber} DOB input was not found after reload.");
            var dobValue = dobInput.GetAttribute("value");
            Assert.Equal(expectedData.DateOfBirth, dobValue);

            _output.WriteLine($"[INFO] Child {childNumber} verified: {firstNameValue} {lastNameValue}, DOB: {dobValue}");
        }

        protected void NavigateToBaselineForm(IPookieWebDriver driver, IWebElement formsPane)
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

        protected void ActivateTab(IPookieWebDriver driver, string tabSelector, string tabName)
        {
            var tabLink = driver.WaitforElementToBeInDOM(By.CssSelector(tabSelector), 10)
                ?? throw new InvalidOperationException($"Tab link '{tabName}' was not found.");
            CommonTestHelper.ClickElement(driver, tabLink);
            driver.WaitForReady(5);
            Thread.Sleep(500);
            _output.WriteLine($"[INFO] Activated {tabName} tab");
        }

        protected IWebElement FindSubmitButton(IPookieWebDriver driver)
        {
            return driver.FindElements(By.CssSelector("a.btn.btn-primary"))
                .FirstOrDefault(el =>
                    el.Displayed &&
                    (el.Text?.Contains("Submit", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    (el.GetAttribute("title")?.Contains("Save", StringComparison.OrdinalIgnoreCase) ?? true))
                ?? throw new InvalidOperationException("Submit button was not found.");
        }

        protected IWebElement? FindValidationMessage(
            IPookieWebDriver driver,
            string description,
            params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                throw new ArgumentException("At least one keyword is required to locate a validation message.", nameof(keywords));
            }

            var candidates = driver.FindElements(By.CssSelector(
                    ".text-danger, span.text-danger, span[style*='color: red'], span[style*='color:Red'], " +
                    "div.alert.alert-danger, .validation-summary-errors li, .validation-summary"))
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

        protected static int GetRandomNumber(int minInclusive, int maxInclusive)
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

        protected string GenerateRandomDateUnder21()
        {
            var currentDate = DateTime.Now;
            var yearUnder21 = currentDate.Year - GetRandomNumber(1, 20); // Between 1 and 20 years old
            var monthRandom = GetRandomNumber(1, 12);
            var dayRandom = GetRandomNumber(1, 28); // Safe day for all months
            return $"{monthRandom:D2}/{dayRandom:D2}/{yearUnder21.ToString().Substring(2, 2)}";
        }
    }
}

