using System;
using System.Linq;
using System.Threading;
using AFUT.Tests.Driver;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AFUT.Tests.Helpers
{
    /// <summary>
    /// Helper methods for interacting with web elements and dropdowns
    /// </summary>
    public static class WebElementHelper
    {
        /// <summary>
        /// Selects a worker from the worker dropdown
        /// </summary>
        public static void SelectWorker(IPookieWebDriver driver, string workerText, string workerValue)
        {
            SelectDropdownOption(
                driver,
                "select#ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder1_ddlWorker, " +
                "select[id$='_ddlWorker'], select[id*='ddlCaseWorker'], select[id*='ddlFSW']",
                "Worker dropdown",
                workerText,
                workerValue);
        }

        /// <summary>
        /// Selects a dropdown option by CSS selector
        /// </summary>
        public static void SelectDropdownOption(IPookieWebDriver driver, string cssSelector, string description, string optionText, string? optionValue)
        {
            var dropdown = FindElementInModalOrPage(driver, cssSelector, description, 15);
            SelectDropdownOption(driver, dropdown, description, optionText, optionValue);
        }

        /// <summary>
        /// Selects a dropdown option from an existing dropdown element
        /// </summary>
        public static void SelectDropdownOption(IPookieWebDriver driver, IWebElement dropdown, string description, string optionText, string? optionValue)
        {
            var select = new SelectElement(dropdown);
            SelectByTextOrValue(select, optionText, optionValue);
            driver.WaitForUpdatePanel(5);
            driver.WaitForReady(5);
            Thread.Sleep(250);
        }

        /// <summary>
        /// Selects an option in a dropdown by text or value (tries text first, then value as fallback)
        /// </summary>
        public static void SelectByTextOrValue(SelectElement selectElement, string optionText, string? optionValue)
        {
            try
            {
                selectElement.SelectByText(optionText);
                return;
            }
            catch (NoSuchElementException)
            {
                // Try value as fallback
            }

            if (!string.IsNullOrWhiteSpace(optionValue))
            {
                selectElement.SelectByValue(optionValue);
                return;
            }

            throw new InvalidOperationException($"Option '{optionText}' was not found in dropdown '{selectElement.WrappedElement?.GetAttribute("id")}'.");
        }

        /// <summary>
        /// Finds an element in a modal or on the page
        /// </summary>
        public static IWebElement FindElementInModalOrPage(IPookieWebDriver driver, string cssSelector, string description, int timeoutSeconds = 10)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

            while (DateTime.Now <= endTime)
            {
                var modal = driver.FindElements(By.CssSelector(".modal.show, .modal.in, .modal[style*='display: block'], .modal.fade.in"))
                    .FirstOrDefault(el => el.Displayed);
                if (modal != null)
                {
                    var withinModal = modal.FindElements(By.CssSelector(cssSelector))
                        .FirstOrDefault(el => el.Displayed);
                    if (withinModal != null)
                    {
                        return withinModal;
                    }
                }

                var fallback = driver.FindElements(By.CssSelector(cssSelector))
                    .FirstOrDefault(el => el.Displayed);
                if (fallback != null)
                {
                    return fallback;
                }

                Thread.Sleep(200);
            }

            throw new InvalidOperationException($"'{description}' was not found within the expected time.");
        }

        /// <summary>
        /// Sets the value of an input field with fallback to JavaScript if needed
        /// </summary>
        public static void SetInputValue(IPookieWebDriver driver, IWebElement input, string value, string fieldDescription, bool triggerBlur = false)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            input.Clear();
            input.SendKeys(value);

            var finalValue = input.GetAttribute("value")?.Trim() ?? string.Empty;
            if (!string.Equals(finalValue, value, StringComparison.OrdinalIgnoreCase))
            {
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
                input.SendKeys(Keys.Tab);
                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].dispatchEvent(new Event('blur', { bubbles: true }));", input);
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Gets the text from a toast/notification message
        /// </summary>
        public static string GetToastMessage(IPookieWebDriver driver, int waitMilliseconds = 1000)
        {
            Thread.Sleep(waitMilliseconds); // Wait for toast to appear
            
            var toastElements = driver.FindElements(By.CssSelector("div.jq-toast-single, div[class*='toast'], div.alert.alert-success"));
            var toastElement = toastElements.FirstOrDefault(el => !string.IsNullOrWhiteSpace(el.Text));

            return toastElement?.Text?.Trim() ?? string.Empty;
        }
    }
}

