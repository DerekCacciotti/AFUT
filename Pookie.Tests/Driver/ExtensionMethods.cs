using OpenQA.Selenium;
using OpenQA.Selenium.DevTools.V104.HeapProfiler;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace AFUT.Tests.Driver
{
    public static class ExtensionMethods
    {
        public static void SwitchToNewTab(this IPookieWebDriver driver)
        {
            driver.SwitchTo().Window(driver.WindowHandles.Last());
        }

        public static void WaitForNewTab(this IPookieWebDriver driver, int timeoutSecs = 5)
        {
            var numOfTabs = driver.WindowHandles.Count;
            var time = DateTime.Now.AddSeconds(timeoutSecs);
            while (driver.WindowHandles.Count() <= numOfTabs || time >= DateTime.Now)
            {
                Thread.Sleep(50);
            }
        }

        public static IWebElement SetDropdownByText(this IWebElement ele, string text, string controlID)
        {
            var dropdown = new SelectElement(ele.FindElement(By.Id(controlID)));
            dropdown.SelectByText(text);
            return ele;
        }

        public static string GetElementValue(this IWebElement ele, [CallerMemberName] string fieldName = "")
        {
            var field = ele.FindElement(By.Name(fieldName));
            return field.GetAttribute("value");
        }

        public static IWebElement SetElementValue(this IWebElement ele, string value, [CallerMemberName] string fieldName = "")
        {
            var field = ele.FindElement(By.Name(fieldName));
            return field.SetValue(value);
        }

        public static IWebElement SetValue(this IWebElement ele, string value)
        {
            ele.SendKeys(value);
            ele.SendKeys(Keys.Tab);
            return ele;
        }

        public static string GetElementValue(this IWebElement ele)
        {
            return ele.GetAttribute("value");
        }

        public static IWebElement GetParent(this IWebElement element)
        {
            return element.FindElement(By.XPath("./.."));
        }

        public static IWebElement WaitforElementToBeInDOM(this ISearchContext page, By by, int timeoutSec = 5)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSec);

            do
            {
                var elements = page.FindElements(by);
                if (elements.Any())
                {
                    return elements.First();
                }

                Thread.Sleep(50);
            } while (DateTime.Now <= endTime);
            return null;
        }

        public static bool WaitForReady(this IPookieWebDriver driver, int timeoutSecs = 5)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSecs);
            var doublecheck = 0;
            do
            {
                if (driver.IsDriving)
                {
                    doublecheck = 0;
                }
                else if (doublecheck++ > 1)
                {
                    return true;
                }
                Thread.Sleep(50);
            }
            while (DateTime.Now <= endTime);

            return false;
        }

        public static bool WaitForElementToLeaveDOM(this IWebDriver driver, By by, int timeoutSecs = 5)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSecs);

            do
            {
                var elements = driver.FindElements(by);
                if (!elements.Any())
                {
                    return true;
                }
            } while (DateTime.Now <= endTime);
            return false;
        }

        public static IWebElement WaitUntilElementCanBeClicked(this IWebDriver driver, By by, int timeoutSeconds = 5)
        {
            try
            {
                return new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds)).Until(ExpectedConditions.ElementToBeClickable(by));
            }
            catch (NoSuchElementException)
            {
                Debug.Write("No such element is the page");
                throw;
            }
        }

        public static bool TryGetElement(this ISearchContext page, By by, out IWebElement element)
        {
            var elements = page.FindElements(by);
            if (elements.Any())
            {
                element = elements.First();
                return true;
            }
            element = null;
            return false;
        }

        public static IAlert WaitForBrowserAlert(this IWebDriver driver, int timeoutSeconds = 5)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            do
            {
                try
                {
                    return driver.SwitchTo().Alert();
                }
                catch (NoAlertPresentException)
                {
                    Thread.Sleep(50);
                }
            } while (DateTime.Now <= endTime);
            throw new TimeoutException("Browser Alert not on screen.");
        }

        public static void CatchUp(this IWebDriver driver, int timeToWaitinMiliseconds = 10000)
        {
            Thread.Sleep(timeToWaitinMiliseconds);
        }

        public static void WaitForUpdatePanel(this IWebDriver driver, int TimeoutSeconds = 30)
        {
            new WebDriverWait(driver, TimeSpan.FromSeconds(TimeoutSeconds)).Until(d =>
            !(bool)(driver as IJavaScriptExecutor).ExecuteScript("return Sys.WebForms.PageRequestManager.getInstance().get_isInAsyncPostBack()"));
        }

        public static IWebElement? GetElementByIDDollarSign(this IWebDriver driver, string controlID)
        {
            return driver.FindElement(By.CssSelector($"[ID$='{controlID}']"));
        }

        public static IWebElement FindElementBySuffix(this IPookieWebDriver driver,
            string baseSelector,
            IEnumerable<string> suffixes,
            string description,
            ITestOutputHelper? output = null)
        {
            return FindElementBySuffixInternal(driver, baseSelector, suffixes, description, output, requireDisplayed: true);
        }

        public static IWebElement FindElementBySuffixIncludingHidden(this IPookieWebDriver driver,
            string baseSelector,
            IEnumerable<string> suffixes,
            string description,
            ITestOutputHelper? output = null)
        {
            return FindElementBySuffixInternal(driver, baseSelector, suffixes, description, output, requireDisplayed: false);
        }

        public static IWebElement FindTextInputBySuffix(this IPookieWebDriver driver, params string[] suffixes)
        {
            return driver.FindTextInputBySuffix(null, suffixes);
        }

        public static IWebElement FindTextInputBySuffix(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] suffixes)
        {
            return driver.FindElementBySuffix("input.form-control", suffixes, "text input", output);
        }

        public static IWebElement FindTextAreaBySuffix(this IPookieWebDriver driver, params string[] suffixes)
        {
            return driver.FindTextAreaBySuffix(null, suffixes);
        }

        public static IWebElement FindTextAreaBySuffix(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] suffixes)
        {
            return driver.FindElementBySuffix("textarea.form-control", suffixes, "text area", output);
        }

        public static IWebElement FindCheckboxBySuffix(this IPookieWebDriver driver, params string[] suffixes)
        {
            return driver.FindCheckboxBySuffix(null, suffixes);
        }

        public static IWebElement FindCheckboxBySuffix(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] suffixes)
        {
            return driver.FindElementBySuffix("input[type='checkbox']", suffixes, "checkbox input", output);
        }

        public static IWebElement FindSelectBySuffix(this IPookieWebDriver driver, params string[] suffixes)
        {
            return driver.FindSelectBySuffix(null, suffixes);
        }

        public static IWebElement FindSelectBySuffix(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] suffixes)
        {
            return driver.FindElementBySuffixIncludingHidden("select", suffixes, "select input", output);
        }

        public static IWebElement FindButtonBySuffix(this IPookieWebDriver driver, params string[] suffixes)
        {
            return driver.FindButtonBySuffix(null, suffixes);
        }

        public static IWebElement FindButtonBySuffix(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] suffixes)
        {
            try
            {
                return driver.FindElementBySuffix("button.btn", suffixes, "button", output);
            }
            catch (InvalidOperationException)
            {
                return driver.FindElementBySuffix("a.btn", suffixes, "button", output);
            }
        }

        public static IWebElement FindValidationSummaryBySuffix(this IPookieWebDriver driver, params string[] suffixes)
        {
            return driver.FindValidationSummaryBySuffix(null, suffixes);
        }

        public static IWebElement FindValidationSummaryBySuffix(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] suffixes)
        {
            return driver.FindElementBySuffix(".validation-summary-errors", suffixes, "validation summary", output);
        }

        public static IWebElement FindLinkByTextFragments(this IPookieWebDriver driver, params string[] textFragments)
        {
            return driver.FindLinkByTextFragments(null, textFragments);
        }

        public static IWebElement FindLinkByTextFragments(this IPookieWebDriver driver, ITestOutputHelper? output, params string[] textFragments)
        {
            textFragments ??= Array.Empty<string>();
            var links = driver.FindElements(By.CssSelector("a, .btn"));

            foreach (var link in links)
            {
                if (!link.Displayed)
                {
                    continue;
                }

                var text = link.Text?.Trim() ?? string.Empty;
                if (textFragments.All(fragment =>
                        text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    output?.WriteLine($"[INFO] Found link containing text '{string.Join(" ", textFragments)}'");
                    return link;
                }
            }

            throw new InvalidOperationException($"Unable to locate link containing text fragments: {string.Join(", ", textFragments)}");
        }

        public static IWebElement FindButtonByText(this IPookieWebDriver driver, string buttonText)
        {
            return driver.FindButtonByText(null, buttonText);
        }

        public static IWebElement FindButtonByText(this IPookieWebDriver driver, ITestOutputHelper? output, string buttonText)
        {
            var match = driver.FindElements(By.CssSelector("button"))
                .FirstOrDefault(el => el.Displayed && MatchesText(el, buttonText));

            if (match != null)
            {
                output?.WriteLine($"[INFO] Found button '{buttonText}'");
                return match;
            }

            match = driver.FindElements(By.CssSelector("input[type='submit'], input[type='button']"))
                .FirstOrDefault(el => el.Displayed && MatchesText(el, buttonText));

            if (match != null)
            {
                output?.WriteLine($"[INFO] Found button '{buttonText}' (input element)");
                return match;
            }

            match = driver.FindElements(By.CssSelector("a.btn"))
                .FirstOrDefault(el => el.Displayed && MatchesText(el, buttonText));

            if (match != null)
            {
                output?.WriteLine($"[INFO] Found button '{buttonText}' (anchor element)");
                return match;
            }

            throw new InvalidOperationException($"Unable to locate button with text '{buttonText}'.");
        }

        private static IWebElement FindElementBySuffixInternal(IPookieWebDriver driver,
            string baseSelector,
            IEnumerable<string> suffixes,
            string description,
            ITestOutputHelper? output,
            bool requireDisplayed)
        {
            foreach (var suffix in suffixes ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(suffix))
                {
                    continue;
                }

                var endsWithSelector = $"{baseSelector}[name$='{suffix}']";
                var match = driver.FindElements(By.CssSelector(endsWithSelector))
                    .FirstOrDefault(el => !requireDisplayed || el.Displayed);
                if (match != null)
                {
                    output?.WriteLine($"[INFO] Found {description} via selector '{endsWithSelector}'");
                    return match;
                }

                endsWithSelector = $"{baseSelector}[id$='{suffix}']";
                match = driver.FindElements(By.CssSelector(endsWithSelector))
                    .FirstOrDefault(el => !requireDisplayed || el.Displayed);
                if (match != null)
                {
                    output?.WriteLine($"[INFO] Found {description} via selector '{endsWithSelector}'");
                    return match;
                }
            }

            throw new InvalidOperationException($"Unable to locate {description} using suffixes: {string.Join(", ", suffixes ?? Array.Empty<string>())}");
        }

        private static bool MatchesText(IWebElement element, string expectedValue)
        {
            var text = element.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text) &&
                text.IndexOf(expectedValue, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var valueAttribute = element.GetAttribute("value")?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(valueAttribute) &&
                   valueAttribute.IndexOf(expectedValue, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}