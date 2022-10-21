using AFUT.Tests.Driver;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Pages
{
    [Page]
    public class GridsPage
    {
        private readonly IPookieWebDriver _driver;
        private IWebElement element;

        public GridsPage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            element = driver.GetElementByIDDollarSign("gvAlbums");
        }

        public void ClickSelectInGrid()
        {
            _driver.WaitForReady();
            var tablerows = element.FindElements(By.CssSelector("tbody tr td"));
            var firstRow = tablerows.FirstOrDefault();
            var button = firstRow.FindElement(By.LinkText("Select"));
            button.Click();
            _driver.WaitForUpdatePanel();
        }
    }
}