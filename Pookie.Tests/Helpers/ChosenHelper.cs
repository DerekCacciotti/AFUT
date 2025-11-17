using System;
using System.Threading;
using AFUT.Tests.Driver;
using OpenQA.Selenium;

namespace AFUT.Tests.Helpers
{
    /// <summary>
    /// Helper class for interacting with Chosen.js dropdown controls.
    /// Chosen is a jQuery plugin that enhances select boxes with search functionality.
    /// </summary>
    public static class ChosenHelper
    {
        /// <summary>
        /// Selects an option in a Chosen dropdown by directly manipulating the underlying select element via JavaScript.
        /// This method is useful when the standard Chosen UI interactions don't work reliably.
        /// 
        /// The method:
        /// 1. Finds the option by value (if provided) or by text
        /// 2. Sets the option as selected
        /// 3. Dispatches change and input events
        /// 4. Triggers jQuery and Chosen-specific events to update the UI
        /// </summary>
        /// <param name="driver">The web driver instance</param>
        /// <param name="selectElement">The underlying select element (typically hidden by Chosen)</param>
        /// <param name="optionText">The text of the option to select (case-insensitive)</param>
        /// <param name="optionValue">Optional value of the option to select. If provided, takes precedence over optionText.</param>
        /// <param name="waitAfterSelection">Time in milliseconds to wait after selection (default: 200ms)</param>
        /// <exception cref="ArgumentNullException">Thrown if driver, selectElement, or optionText is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if the option is not found</exception>
        public static void SelectOptionByScript(
            IPookieWebDriver driver,
            IWebElement selectElement,
            string optionText,
            string? optionValue = null,
            int waitAfterSelection = 200)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (selectElement == null)
                throw new ArgumentNullException(nameof(selectElement));
            if (string.IsNullOrWhiteSpace(optionText))
                throw new ArgumentNullException(nameof(optionText));

            var js = (IJavaScriptExecutor)driver;

            js.ExecuteScript(@"
                var select = arguments[0];
                var targetText = (arguments[1] || '').trim().toLowerCase();
                var targetValue = (arguments[2] || '').trim();
                var option = Array.from(select.options).find(function(opt) {
                    if (targetValue && opt.value === targetValue) {
                        return true;
                    }
                    return opt.text && opt.text.trim().toLowerCase() === targetText;
                });

                if (!option) {
                    throw new Error('Option not found: ' + (targetText || targetValue));
                }

                option.selected = true;
                if (!select.multiple) {
                    Array.from(select.options).forEach(function(opt) {
                        if (opt !== option) {
                            opt.selected = false;
                        }
                    });
                }

                var changeEvent = new Event('change', { bubbles: true });
                select.dispatchEvent(changeEvent);
                var inputEvent = new Event('input', { bubbles: true });
                select.dispatchEvent(inputEvent);

                if (window.jQuery) {
                    window.jQuery(select).trigger('change');
                    window.jQuery(select).trigger('chosen:updated');
                }
            ", selectElement, optionText, optionValue ?? string.Empty);

            Thread.Sleep(waitAfterSelection);
        }
    }
}

