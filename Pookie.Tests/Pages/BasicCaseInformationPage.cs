using System;
using System.Collections.Generic;
using System.Linq;
using AFUT.Tests.Driver;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AFUT.Tests.Pages
{
    [Page(Name = "Basic Case Information", Category = "Case Home")]
    public class BasicCaseInformationPage
    {
        private static readonly By FormSelector = By.CssSelector("form[action*='BasicCaseInformation.aspx']");
        private static readonly By EditInformationButtonSelector = By.XPath("//a[contains(@class,'btn') and contains(normalize-space(.), 'Edit Information')]");
        private static readonly By SubmitButtonSelector = By.CssSelector("a[id$='btnSubmit']");
        private static readonly By CancelButtonSelector = By.CssSelector("a[id$='btnCancel']");
            private static readonly By CaseHomeFormSelector = By.XPath("//form[contains(translate(@action,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'casehome.aspx')]");

        private static readonly IReadOnlyDictionary<BasicCaseInformationField, By> FieldSelectors = new Dictionary<BasicCaseInformationField, By>
        {
            { BasicCaseInformationField.AlternateId, By.CssSelector("input[id$='txtAlternateID']") },
            { BasicCaseInformationField.ScreenDate, By.CssSelector("input[id$='txtScreenDate']") },
            { BasicCaseInformationField.TargetChildDob, By.CssSelector("input[id$='txtTCDOB']") },
            { BasicCaseInformationField.IntakeDate, By.CssSelector("input[id$='txtIntakeDate']") },
            { BasicCaseInformationField.ParentSurveyDate, By.CssSelector("input[id$='txtKempeDate']") }
        };

        private static readonly BasicCaseInformationField[] EditableFieldDefinitions =
        {
            BasicCaseInformationField.AlternateId,
            BasicCaseInformationField.ScreenDate,
            BasicCaseInformationField.TargetChildDob,
            BasicCaseInformationField.IntakeDate,
            BasicCaseInformationField.ParentSurveyDate
        };

        private readonly IPookieWebDriver _driver;
        private IWebElement _form;

        public BasicCaseInformationPage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.WaitForReady(30);
            _form = _driver.WaitforElementToBeInDOM(FormSelector, 30)
                     ?? throw new InvalidOperationException("Basic Case Information form was not found.");
        }

        public bool IsLoaded => _form.Displayed;

        public IReadOnlyList<BasicCaseInformationField> EditableFields => EditableFieldDefinitions;

        public void EnterEditMode(int timeoutSeconds = 30)
        {
            if (EditableFieldDefinitions.All(field => SafeIsFieldEditable(field)))
            {
                return;
            }

            IWebElement? editButton = null;

            try
            {
                editButton = _driver.WaitUntilElementCanBeClicked(EditInformationButtonSelector, timeoutSeconds);
            }
            catch (NoSuchElementException)
            {
                editButton = null;
            }
            catch (WebDriverTimeoutException)
            {
                editButton = null;
            }

            if (editButton is not null && !IsDisabledControl(editButton))
            {
                _driver.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", editButton);
                editButton.Click();

                _driver.WaitForReady(timeoutSeconds);
                RefreshFormReference(timeoutSeconds);
            }

            foreach (var field in EditableFieldDefinitions)
            {
                WaitForFieldToBeEditable(field, timeoutSeconds);
            }
        }

        public bool IsFieldEditable(BasicCaseInformationField field)
        {
            var element = ResolveFieldElement(field, 5);
            return IsElementEditable(element);
        }

        public string GetFieldValue(BasicCaseInformationField field)
        {
            var element = ResolveFieldElement(field, 5);
            return element.GetAttribute("value")?.Trim() ?? string.Empty;
        }

        public void SetFieldValue(BasicCaseInformationField field, string value)
        {
            var element = ResolveFieldElement(field, 5);

            if (!IsElementEditable(element))
            {
                throw new InvalidOperationException($"Field '{field}' is not editable.");
            }

            _driver.ExecuteScript("arguments[0].focus();", element);
            element.Click();
            element.SendKeys(Keys.Control + "a");
            element.SendKeys(Keys.Delete);

            if (!string.IsNullOrEmpty(value))
            {
                element.SendKeys(value);
            }

            element.SendKeys(Keys.Tab);
            _driver.WaitForReady(5);
        }

        public CaseHomePage SubmitChanges(int timeoutSeconds = 30)
        {
            var submit = _driver.WaitUntilElementCanBeClicked(SubmitButtonSelector, timeoutSeconds)
                        ?? throw new InvalidOperationException("Submit button was not found on the Basic Case Information page.");

            submit.Click();
            _driver.WaitForReady(timeoutSeconds);
            return new CaseHomePage(_driver);
        }

        public CaseHomePage CancelChanges(int timeoutSeconds = 30)
        {
            var cancel = _driver.WaitUntilElementCanBeClicked(CancelButtonSelector, timeoutSeconds);
            if (cancel is null)
            {
                throw new InvalidOperationException("Cancel button was not found on the Basic Case Information page.");
            }

            cancel.Click();
            _driver.WaitForReady(timeoutSeconds);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(d =>
            {
                try
                {
                    return d.Url.Contains("CaseHome.aspx", StringComparison.OrdinalIgnoreCase);
                }
                catch (WebDriverException)
                {
                    return false;
                }
            });

            _ = _driver.WaitforElementToBeInDOM(CaseHomeFormSelector, timeoutSeconds)
                ?? throw new InvalidOperationException("Case Home form was not found after cancelling Basic Case Information edits.");

            return new CaseHomePage(_driver);
        }

        private void RefreshFormReference(int timeoutSeconds)
        {
            _form = _driver.WaitforElementToBeInDOM(FormSelector, timeoutSeconds)
                     ?? throw new InvalidOperationException("Basic Case Information form was not found after navigation.");
        }

        private void WaitForFieldToBeEditable(BasicCaseInformationField field, int timeoutSeconds)
        {
            var selector = FieldSelectors[field];
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));

            wait.Until(_ =>
            {
                try
                {
                    var input = _driver.FindElement(selector);
                    return IsElementEditable(input);
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
        }

        private IWebElement ResolveFieldElement(BasicCaseInformationField field, int timeoutSeconds)
        {
            if (!FieldSelectors.TryGetValue(field, out var selector))
            {
                throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown Basic Case Information field requested.");
            }

            var element = _driver.WaitforElementToBeInDOM(selector, timeoutSeconds);
            if (element is null)
            {
                throw new InvalidOperationException($"Field '{field}' was not found on the Basic Case Information page.");
            }

            return element;
        }

        private static bool IsElementEditable(IWebElement element)
        {
            if (element is null)
            {
                return false;
            }

            if (!element.Enabled)
            {
                return false;
            }

            var readOnly = element.GetAttribute("readonly");
            if (!string.IsNullOrWhiteSpace(readOnly) && !string.Equals(readOnly, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var ariaDisabled = element.GetAttribute("aria-disabled");
            if (!string.IsNullOrWhiteSpace(ariaDisabled) && !string.Equals(ariaDisabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var disabled = element.GetAttribute("disabled");
            if (!string.IsNullOrWhiteSpace(disabled) && !string.Equals(disabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsDisabledControl(IWebElement element)
        {
            if (element is null)
            {
                return true;
            }

            var disabledAttribute = element.GetAttribute("disabled");
            if (!string.IsNullOrWhiteSpace(disabledAttribute) && !string.Equals(disabledAttribute, "false", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var ariaDisabled = element.GetAttribute("aria-disabled");
            if (!string.IsNullOrWhiteSpace(ariaDisabled) && !string.Equals(ariaDisabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var classAttribute = element.GetAttribute("class");
            if (!string.IsNullOrWhiteSpace(classAttribute))
            {
                var classes = classAttribute.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (classes.Any(c => string.Equals(c, "disabled", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool SafeIsFieldEditable(BasicCaseInformationField field)
        {
            try
            {
                var element = ResolveFieldElement(field, 5);
                return IsElementEditable(element);
            }
            catch
            {
                return false;
            }
        }

        public enum BasicCaseInformationField
        {
            AlternateId,
            ScreenDate,
            TargetChildDob,
            IntakeDate,
            ParentSurveyDate
        }
    }
}

