using System;
using System.Linq;
using AFUT.Tests.Driver;
using System.Threading;
using OpenQA.Selenium;

namespace AFUT.Tests.Pages
{
    [Page]
    public class SelectRolePage
    {
        private static readonly By RoleGridSelector = By.CssSelector("table[id$='grvProgramRoles']");
        private static readonly By RoleRowSelector = By.CssSelector("tbody tr");
        private static readonly By SelectLinkSelector = By.LinkText("Select");
        private readonly IPookieWebDriver _driver;

        public SelectRolePage(IPookieWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));

            _driver.WaitForReady(30);
            var grid = _driver.WaitforElementToBeInDOM(RoleGridSelector, 30)
                ?? throw new InvalidOperationException("Role selection grid is not available on the current page.");
            if (!grid.Displayed)
            {
                throw new InvalidOperationException("Role selection grid is not visible.");
            }
        }

        public IAppLandingPage SelectRole(string programName, string roleName)
        {
            if (string.IsNullOrWhiteSpace(programName))
            {
                throw new ArgumentException("Program name must be provided.", nameof(programName));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("Role name must be provided.", nameof(roleName));
            }

            var targetRow = FindRow(programName, roleName);
            var isAdminRole = string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
            targetRow.FindElement(By.LinkText("Select")).Click();

            _driver.WaitForReady(60);

            var roleLabel = _driver.WaitforElementToBeInDOM(By.CssSelector("[id$='lblUserRole']"), 60)
                ?? throw new InvalidOperationException("Navigation menu did not render after selecting role.");

            var resolvedRoleName = WaitForRoleLabel(roleLabel, 30);
            if (!string.IsNullOrWhiteSpace(resolvedRoleName) &&
                !string.Equals(resolvedRoleName, roleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected role '{roleName}' but landed on role '{resolvedRoleName}'.");
            }

            var homeIndicator = _driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='hfUsername']"), 60)
                               ?? _driver.WaitforElementToBeInDOM(By.CssSelector("[id$='divButton'], [id$='pnlDashboards']"), 30);
            if (homeIndicator is null)
            {
                throw new InvalidOperationException("Home page content did not render after selecting role.");
            }

            return isAdminRole ? new AdminHomePage(_driver) : new HomePage(_driver);
        }

        public IAppLandingPage SelectFirstAvailableRole()
        {
            var roleGrid = _driver.WaitforElementToBeInDOM(RoleGridSelector, 30)
                ?? throw new InvalidOperationException("Role selection grid is not available on the current page.");

            var rows = roleGrid.FindElements(RoleRowSelector)
                       ?? throw new InvalidOperationException("Role selection grid rows were not found.");

            var firstDataRow = rows.FirstOrDefault();
            if (firstDataRow is null)
            {
                throw new InvalidOperationException("Role selection grid did not contain any roles to select.");
            }

            var cells = firstDataRow.FindElements(By.TagName("td"));
            if (cells.Count < 3)
            {
                throw new InvalidOperationException("Role selection grid row did not contain expected cells.");
            }

            var programName = cells[1].Text?.Trim() ?? string.Empty;
            var roleName = cells[2].Text?.Trim() ?? string.Empty;

            var selectLink = firstDataRow.FindElements(SelectLinkSelector).FirstOrDefault()
                             ?? throw new InvalidOperationException("Role selection row does not have a selectable link.");

            selectLink.Click();

            _driver.WaitForReady(60);

            var roleLabel = _driver.WaitforElementToBeInDOM(By.CssSelector("[id$='lblUserRole']"), 60)
                ?? throw new InvalidOperationException("Navigation menu did not render after selecting role.");

            var resolvedRoleName = WaitForRoleLabel(roleLabel, 30);
            if (!string.IsNullOrWhiteSpace(roleName) &&
                !string.IsNullOrWhiteSpace(resolvedRoleName) &&
                !string.Equals(resolvedRoleName, roleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected role '{roleName}' but landed on role '{resolvedRoleName}'.");
            }

            var homeIndicator = _driver.WaitforElementToBeInDOM(By.CssSelector("input[id$='hfUsername']"), 60)
                               ?? _driver.WaitforElementToBeInDOM(By.CssSelector("[id$='divButton'], [id$='pnlDashboards']"), 30);
            if (homeIndicator is null)
            {
                throw new InvalidOperationException("Home page content did not render after selecting role.");
            }

            var isAdminRole = string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
            return isAdminRole ? new AdminHomePage(_driver) : new HomePage(_driver);
        }

        private IWebElement FindRow(string programName, string roleName)
        {
            var roleGrid = _driver.WaitforElementToBeInDOM(RoleGridSelector, 30)
                ?? throw new InvalidOperationException("Role selection grid is not available on the current page.");

            var rows = roleGrid.FindElements(RoleRowSelector);

            var targetRow = rows.FirstOrDefault(row =>
            {
                var cells = row.FindElements(By.TagName("td"));
                if (cells.Count < 3)
                {
                    return false;
                }

                var programText = cells[1].Text?.Trim();
                var roleText = cells[2].Text?.Trim();

                return string.Equals(programText, programName, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(roleText, roleName, StringComparison.OrdinalIgnoreCase);
            });

            if (targetRow is null)
            {
                throw new InvalidOperationException($"Unable to locate role '{roleName}' for program '{programName}'.");
            }

            return targetRow;
        }

        private static string WaitForRoleLabel(IWebElement roleLabel, int timeoutSeconds)
        {
            var endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            string text;

            do
            {
                text = roleLabel.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }

                Thread.Sleep(100);
            }
            while (DateTime.UtcNow <= endTime);

            return string.Empty;
        }
    }
}

