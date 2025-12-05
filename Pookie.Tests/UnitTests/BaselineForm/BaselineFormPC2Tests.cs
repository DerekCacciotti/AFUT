using System;
using System.Linq;
using AFUT.Tests.Config;
using AFUT.Tests.Driver;
using AFUT.Tests.UnitTests.Attributes;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace AFUT.Tests.UnitTests.BaselineForm
{
    /// <summary>
    /// Validation tests for PC2 (Primary Caregiver 2) tab of the Baseline Form.
    /// Inherits all test logic from BaselineFormValidationTests and only overrides form-specific tokens.
    /// </summary>
    [TestCaseOrderer("AFUT.Tests.UnitTests.Attributes.PriorityOrderer", "AFUT.Tests")]
    public class BaselineFormPC2Tests : BaselineFormValidationTests
    {
        protected override string FormToken => "PC2Form";
        protected override string TabSelector => "#tab_PC2 a[href='#PC2']";
        protected override bool CheckConsistencyValidation => false;

        public BaselineFormPC2Tests(AppConfig config, ITestOutputHelper output)
            : base(config, output)
        {
        }

        protected override void ClickSubmitButton(IPookieWebDriver driver)
        {
            // Call base class submit logic
            base.ClickSubmitButton(driver);

            // PC2 bug: After any submit, page switches back to PC1 tab - switch back to PC2
            // But only if the tab still exists (it might disappear after successful save)
            var tabLink = driver.FindElements(By.CssSelector(TabSelector)).FirstOrDefault();
            if (tabLink != null && tabLink.Displayed)
            {
                _output.WriteLine("[INFO] Switching back to PC2 tab after submit");
                ActivateTab(driver);
            }
            else
            {
                _output.WriteLine("[INFO] PC2 tab not found after submit (likely successful save)");
            }
        }
    }
}

