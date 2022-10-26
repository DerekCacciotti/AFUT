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

        public bool ClickSelectInGrid()
        {
            _driver.WaitForReady();
            var tablecells = element.FindElements(By.CssSelector("tbody tr td"));
            var firstCell = tablecells.FirstOrDefault();
            var button = firstCell.FindElement(By.LinkText("Select"));
            button.Click();
            _driver.WaitForReady();
            var tableRows = _driver.FindElements(By.CssSelector("tbody tr"));
            var firstDataRow = tableRows[1];
            return firstDataRow.GetAttribute("class") == "success";
        }
    }
}