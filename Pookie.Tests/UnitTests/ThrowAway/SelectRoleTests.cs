using AFUT.Tests.Driver;
using AFUT.Tests.Pages;
using Xunit;

namespace AFUT.Tests.UnitTests.ThrowAway
{
    [Collection("Select Role Collection")]
    public class SelectRoleTests
    {
        private readonly SelectRoleFixture fixture;

        public SelectRoleTests(SelectRoleFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void User_Can_Select_DataEntry_Role()
        {
            var selectRolePage = fixture.RefreshSelectRolePage();
            var landingPage = selectRolePage.SelectRole("Program 1", "DataEntry");

            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded);
            Assert.IsType<HomePage>(landingPage);
        }

        [Fact]
        public void User_Can_Select_Admin_Role()
        {
            var selectRolePage = fixture.RefreshSelectRolePage();
            var landingPage = selectRolePage.SelectRole("Program 2", "Admin");

            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded);
            Assert.IsType<AdminHomePage>(landingPage);
        }

        [Fact]
        public void User_Can_Open_SearchCases_Dropdown_And_Open_First_Case()
        {
            var landingPage = fixture.EnsureLandingPage();

            Assert.NotNull(landingPage);
            Assert.True(landingPage.IsLoaded);

            var navigationBar = new NavigationBar(fixture.Driver);
            var caseHomePage = navigationBar.OpenFirstRecentCaseFromSearchCasesDropdown();

            Assert.True(caseHomePage.IsLoaded);
            Assert.False(string.IsNullOrWhiteSpace(caseHomePage.PC1Id));
        }
    }
}

