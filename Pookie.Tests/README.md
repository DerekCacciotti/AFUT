# Pookie.Tests

Automation-focused test project that exercises core Search Cases workflows for the Pookie web application using Selenium, xUnit, and a small routine framework.

## Getting Started

1. **Install prerequisites**
   - .NET SDK 6.0+
   - Google Chrome (stable channel)
2. **Configure credentials and target URL**
   - Update `appsettings.json` with non-secret defaults (e.g., base URLs that are safe to commit)
   - Provide secrets via [User Secrets](https://learn.microsoft.com/dotnet/core/extensions/user-secrets) or environment variables prefixed with `POOKIE_` (`POOKIE_UserName`, `POOKIE_Password`)
3. **Restore and build**
   ```powershell
   dotnet restore
   dotnet build
   ```
4. **Run the test suite**
   ```powershell
   dotnet test
   ```

`PookieDriverFactory` downloads and caches a matching ChromeDriver version on demand, so no manual binary management is required.

## Project Layout

- `Pages/` – Page Object models encapsulating UI interactions (`SearchCasesPage`, `HomePage`, etc.)
- `Routine/` – Reusable workflow routines composed of annotated steps and optional output markers
- `UnitTests/` – xUnit test fixtures that bind drivers, routines, and assertions
- `Driver/` – Selenium driver factory and wrappers (`IPookieWebDriver`, `DriverWrapper`)
- `Config/` – `AppConfig` bootstrap supplying configuration and dependency injection
- `Attributes/`, `Helpers/`, `Seeder/` – Supporting infrastructure for generating data and capturing routine output

## Test Suite

### Search Cases

`UnitTests/SearchCases/SearchCasesTests.cs` covers the primary end-to-end flows using `SearchCasesSearchRoutine`:

- `Fill_All_Search_Fields_DisplaysMatchingResult` – verifies a fully populated query returns a matching result row
- `Fill_All_Search_Fields_OpensMatchingCaseHome` – ensures the first result opens the matching case home page
- `Search_With_No_Criteria_DisplaysNoRecordsFoundMessage` – checks that an empty search shows the “No records found.” banner
- `Cancel_Search_Returns_To_Home_Page` – confirms canceling the search brings the user back to the home landing page

Each test acquires a browser via `IPookieDriverFactory`, executes the same routine steps, and asserts against `Routine.Params` outputs (e.g., `SignedIn`, `SearchCompleted`) to ensure the workflow succeeds.

### ThrowAway Samples

`UnitTests/ThrowAway` contains exploratory fixtures for legacy WebForms components (`WebFormsTests`, `WebFormsLoginTests`, `SelectRoleTests`). These are useful references for additional interactions but are not part of the primary Search Cases regression pack.

## Routines and Outputs

- Routines are classes tagged with `[Routine]` that orchestrate reusable UI flows. Methods marked with `[RoutineStep]` define the ordered steps.
- `SearchCasesSearchRoutine` demonstrates a full workflow: loading the app, authenticating, selecting a role, navigating to Search Cases, populating criteria, and submitting the search. The inner `Params` class captures inputs (criteria, desired role) and outputs decorated with `[RoutineOutput]` (sign-in state, first result ID, etc.).
- `SetUp.For<T>()` populates routine parameter objects using `ValueGeneratorAttribute` hints, and `RoutineOutputAttribute` marks fields to expose during assertions.

When extending or creating new routines:

- Define a `[Routine]` class inside `Routine/` (grouped by feature area)
- Expose a static `GetParameters()` helper that seeds defaults via `SetUp.For`
- Model inputs/outputs on a nested `Params` class; use `[RoutineOutput]` for any value you plan to assert
- Break the flow into `[RoutineStep(stepNumber, "Description")]` methods that encapsulate atomic browser interactions

## Adding More Tests

- Create new xUnit fixtures under `UnitTests/<Feature>/`
- Request a driver via DI-enabled `AppConfig` (fixture) and always dispose it with `using`
- Reuse existing routines when possible; call step methods explicitly to keep assertions transparent
- For new pages, follow the Page Object pattern already in `Pages/` and lean on `IPookieWebDriver` helper extensions (`WaitForReady`, `WaitforElementToBeInDOM`)
- Add deterministic data via routine parameters or extend `SetUp`/`ValueGeneratorAttribute` implementations if generation helpers are required

Sample test skeleton:

```csharp
public class MyFeatureTests : IClassFixture<AppConfig>
{
    private readonly IPookieDriverFactory _driverFactory;

    public MyFeatureTests(AppConfig config)
    {
        _driverFactory = config.ServiceProvider.GetRequiredService<IPookieDriverFactory>();
    }

    [Fact]
    public void ScenarioName()
    {
        using var driver = _driverFactory.CreateDriver();
        var routine = new MyFeatureRoutine(driver, config);
        var parms = MyFeatureRoutine.GetParameters();

        // routine steps here...

        Assert.True(parms.ExpectedFlag);
    }
}
```

## Helpful Commands

- Run only Search Cases tests:
  ```powershell
  dotnet test --filter FullyQualifiedName~SearchCasesTests
  ```
- Capture coverage (uses built-in coverlet collector):
  ```powershell
  dotnet test /p:CollectCoverage=true
  ```
- Update ChromeDriver cache manually (rarely needed): delete folders under `ChromeDriver/` and rerun the suite

## Troubleshooting

- If authentication fails, confirm secrets are available to the test process and the configured user has access to the requested program/role
- Ensure Chrome auto-updates do not outpace driver downloads; rerunning `dotnet test` triggers a fresh driver fetch when versions differ
- For flaky waits, review `IPookieWebDriver` wrappers in `Driver/ExtensionMethods.cs` to add or adjust synchronization helpers


