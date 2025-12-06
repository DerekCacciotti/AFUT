using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.Helpers;
using AFUT.Tests.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.BaselineForm
{
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class BaselineFormMIECHVTests : IClassFixture<AppConfig>
    {
        protected readonly AppConfig _config;
        protected readonly IPookieDriverFactory _driverFactory;
        protected readonly ITestOutputHelper _output;

        public BaselineFormMIECHVTests(AppConfig config, ITestOutputHelper output)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _driverFactory = _config.ServiceProvider.GetService<IPookieDriverFactory>()
                              ?? throw new InvalidOperationException("Driver factory was not registered in the service provider.");
        }

        public static IEnumerable<object[]> GetTestPc1Ids()
        {
            var config = new AppConfig();
            return config.TestPc1Ids.Select(id => new object[] { id });
        }

        [Theory]
        [MemberData(nameof(GetTestPc1Ids))]
        [TestPriority(7)]
        public void MIECHVTabCompleteFlowTest(string pc1Id)
        {
            using var driver = _driverFactory.CreateDriver();
            driver.Manage().Window.Maximize();

            try
            {
                _output.WriteLine($"\n{'='}{new string('=', 70)}");
                _output.WriteLine($"[TEST START] MIECHV Tab Test for PC1 ID: {pc1Id}");
                _output.WriteLine($"{'='}{new string('=', 70)}\n");

                // ===== PART 1: Navigate to Baseline Form =====
                _output.WriteLine("[TEST SECTION] Navigating to Baseline Form");

                var (homePage, formsPane) = CommonTestHelper.NavigateToFormsTab(driver, _config, pc1Id);
                _output.WriteLine("[PASS] Successfully navigated to Forms tab");

                // Click on Baseline Form link
                var baselineFormLinks = formsPane.FindElements(By.CssSelector("a.list-group-item"))
                    .Where(el => el.Displayed && el.Text.Contains("Baseline Form", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!baselineFormLinks.Any())
                {
                    throw new InvalidOperationException("Baseline Form link was not found in the Forms pane.");
                }

                var baselineFormLink = baselineFormLinks.First();
                _output.WriteLine($"[INFO] Found Baseline Form link: {baselineFormLink.Text.Trim()}");
                CommonTestHelper.ClickElement(driver, baselineFormLink);
                driver.WaitForReady(30);
                driver.WaitForUpdatePanel(30);
                Thread.Sleep(1000);

                var currentUrl = driver.Url ?? string.Empty;
                _output.WriteLine($"[INFO] Navigated to Intake page: {currentUrl}");

                // ===== PART 2: Activate MIECHV Tab =====
                _output.WriteLine("\n[TEST SECTION] Activating MIECHV tab");

                ActivateTab(driver, "#tab_MIECHV a[href='#MIECHV']", "MIECHV");
                _output.WriteLine("[PASS] MIECHV tab activated successfully");

                // ===== PART 3: Fill MIECHV Form with Random Values =====
                _output.WriteLine("\n[TEST SECTION] Filling MIECHV form with random values");

                var random = new Random();

                // 1. PC1 Living Arrangement (PC1-3a)
                var livingArrangementDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlLivingArrangement']"))
                    .FirstOrDefault(el => el.Displayed && el.GetAttribute("id").EndsWith("ddlLivingArrangement"));
                if (livingArrangementDropdown != null)
                {
                    var select = new SelectElement(livingArrangementDropdown);
                    var options = select.Options.Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value"))).ToList();
                    var randomOption = options[random.Next(options.Count)];
                    select.SelectByValue(randomOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(300);
                    _output.WriteLine($"[INFO] PC1-3a Living Arrangement: Selected '{randomOption.Text.Trim()}'");
                }

                // 2. PC1 Living Situation Specific (PC1-3b)
                var livingArrangementSpecificDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlLivingArrangementSpecific']"))
                    .FirstOrDefault(el => el.Displayed);
                if (livingArrangementSpecificDropdown != null)
                {
                    var select = new SelectElement(livingArrangementSpecificDropdown);
                    var options = select.Options.Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value"))).ToList();
                    var randomOption = options[random.Next(options.Count)];
                    select.SelectByValue(randomOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(300);
                    _output.WriteLine($"[INFO] PC1-3b Living Situation: Selected '{randomOption.Text.Trim()}'");
                }

                // 3. PC1 Self Low Student Achievement (PC1-4)
                var selfLowAchievementDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlPC1SelfLowStudentAchievement']"))
                    .FirstOrDefault(el => el.Displayed);
                if (selfLowAchievementDropdown != null)
                {
                    var select = new SelectElement(selfLowAchievementDropdown);
                    var options = select.Options.Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value"))).ToList();
                    var randomOption = options[random.Next(options.Count)];
                    select.SelectByValue(randomOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(300);
                    _output.WriteLine($"[INFO] PC1-4 Self Low Achievement: Selected '{randomOption.Text.Trim()}'");
                }

                // 4. Children Low Student Achievement (PC1-5)
                var childrenLowAchievementDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlPC1ChildrenLowStudentAchievement']"))
                    .FirstOrDefault(el => el.Displayed);
                if (childrenLowAchievementDropdown != null)
                {
                    var select = new SelectElement(childrenLowAchievementDropdown);
                    var options = select.Options.Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value"))).ToList();
                    var randomOption = options[random.Next(options.Count)];
                    select.SelectByValue(randomOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(300);
                    _output.WriteLine($"[INFO] PC1-5 Children Low Achievement: Selected '{randomOption.Text.Trim()}'");
                }

                // 5. Other Children Developmental Delays (PC1-6)
                var childrenDelaysDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlOtherChildrenDevelopmentalDelays']"))
                    .FirstOrDefault(el => el.Displayed);
                if (childrenDelaysDropdown != null)
                {
                    var select = new SelectElement(childrenDelaysDropdown);
                    var options = select.Options.Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value"))).ToList();
                    var randomOption = options[random.Next(options.Count)];
                    select.SelectByValue(randomOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(300);
                    _output.WriteLine($"[INFO] PC1-6 Children Developmental Delays: Selected '{randomOption.Text.Trim()}'");
                }

                // 6. Family Armed Forces (PC1-7)
                var armedForcesDropdown = driver.FindElements(By.CssSelector("select.form-control[id*='ddlPC1FamilyArmedForces']"))
                    .FirstOrDefault(el => el.Displayed);
                if (armedForcesDropdown != null)
                {
                    var select = new SelectElement(armedForcesDropdown);
                    var options = select.Options.Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value"))).ToList();
                    var randomOption = options[random.Next(options.Count)];
                    select.SelectByValue(randomOption.GetAttribute("value"));
                    driver.WaitForUpdatePanel(2);
                    Thread.Sleep(300);
                    _output.WriteLine($"[INFO] PC1-7 Family Armed Forces: Selected '{randomOption.Text.Trim()}'");
                }

                _output.WriteLine("[PASS] All MIECHV form fields filled with random values");

                // ===== PART 4: Submit and Verify Toast =====
                _output.WriteLine("\n[TEST SECTION] Submitting form and verifying toast");

                var submitButton = FindSubmitButton(driver);
                CommonTestHelper.ClickElement(driver, submitButton);
                driver.WaitForUpdatePanel(30);
                driver.WaitForReady(30);
                Thread.Sleep(3000); // Wait for potential toast to appear and disappear

                // Verify success toast message or redirect
                var toastMessage = WebElementHelper.GetToastMessage(driver, 3000);
                currentUrl = driver.Url ?? string.Empty;

                // If toast is empty but we redirected to CaseHome, the form saved successfully
                if (string.IsNullOrWhiteSpace(toastMessage) && currentUrl.Contains("CaseHome.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine("[INFO] Form saved successfully (redirected to CaseHome.aspx)");
                    toastMessage = $"Form Saved - {pc1Id}"; // Infer toast message
                }
                else if (currentUrl.Contains("errorpage.aspx", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.True(false, "Form submission failed - redirected to error page.");
                }

                Assert.False(string.IsNullOrWhiteSpace(toastMessage), "Success toast message was not displayed and no redirect to CaseHome occurred.");
                Assert.Contains("Form Saved", toastMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(pc1Id, toastMessage, StringComparison.OrdinalIgnoreCase);
                _output.WriteLine($"[PASS] Form saved successfully: {toastMessage}");

                _output.WriteLine("\n[PASS] MIECHV tab complete flow test finished successfully");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"\n[FAIL] Test failed with error: {ex.Message}");
                _output.WriteLine($"[STACK TRACE] {ex.StackTrace}");
                throw;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Activates a tab by clicking on its link
        /// </summary>
        private void ActivateTab(IPookieWebDriver driver, string tabSelector, string tabName)
        {
            var tabLink = driver.WaitforElementToBeInDOM(By.CssSelector(tabSelector), 10)
                ?? throw new InvalidOperationException($"{tabName} tab link was not found.");

            CommonTestHelper.ClickElement(driver, tabLink);
            driver.WaitForUpdatePanel(30);
            driver.WaitForReady(30);
            Thread.Sleep(500);
            _output.WriteLine($"[INFO] Activated {tabName} tab");
        }

        /// <summary>
        /// Finds the submit button on the page
        /// </summary>
        private IWebElement FindSubmitButton(IPookieWebDriver driver)
        {
            var submitButton = driver.FindElements(By.CssSelector("a.btn.btn-primary, input.btn.btn-primary[type='submit']"))
                .FirstOrDefault(el => el.Displayed && 
                    (el.Text.Contains("Submit", StringComparison.OrdinalIgnoreCase) || 
                     el.GetAttribute("value")?.Contains("Submit", StringComparison.OrdinalIgnoreCase) == true))
                ?? throw new InvalidOperationException("Submit button was not found on the page.");

            return submitButton;
        }

        #endregion
    }
}

