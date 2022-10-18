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
            var endTime = DateTime.Now.AddSeconds(5);

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
    }
}