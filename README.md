# Percy C# Selenium

[Percy](https://percy.io) visual testing for C#.NET Selenium. (Supports both .NET Core and .NET Framework - 4.5 to 4.8)

## Requirements

* [Node JS](https://nodejs.org/en/)
* [Chrome Driver](https://chromedriver.chromium.org/)

For other systems (or installation alternatives), see:
https://github.com/SeleniumHQ/selenium/wiki/ChromeDriver


## Installation

### Install `percy-csharp-selenium`

Refer [NuGet](https://www.nuget.org/packages/percy-csharp-selenium)

## Usage

This is an example test using the percy.Snapshot function.

```c#

using percy_csharp_selenium;

public class Example{
  public IWebDriver driver;
  public Percy percy;
  
  public void Init(){
    ChromeOptions options = new ChromeOptions();
    options.AddArguments("--headless");
    driver = new ChromeDriver(options);
    percy = new Percy();
  }
  
  public void TestCase(){
    driver.Navigate().GoToUrl("https://browserstack.com");
    percy.Snapshot(driver,"Home Page", null);
  }
  
  public void TestCaseWithOptions(){
    driver.Navigate().GoToUrl("https://browserstack.com/docs");
    percy.Snapshot(driver,"Docs Page", new Dictionary<string, object> {
                {"widths", new List<int> { 768, 992, 1200 }},
                { "minHeight", 1200 },
                { "enableJavaScript",  false },
                { "percyCSS", ".clear-completed { visibility: hidden; }" }
                });
  }

}

```

Running the test above normally will result in the following log:

```bash

[percy] Percy is not running, disabling snapshots

```

When running with percy exec, and your project's PERCY_TOKEN, a new Percy build will be created and snapshots will be uploaded to your project.

```

$ export PERCY_TOKEN=[your-project-token]
$ npx percy exec -- [c# test command]
[percy] Percy has started!
[percy] Created build #1: https://percy.io/[your-project]
[percy] Snapshot taken "Home Page"
[percy] Snapshot taken "Docs Page"
[percy] Stopping percy...
[percy] Finalized build #1: https://percy.io/[your-project]
[percy] Done!

```

## Configurations 

The snapshot method arguments:

`percy.Snapshot(IWebDriver driver, String name, Dictionary<string, object> options)`
* name (required) - The snapshot name; must be unique to each snapshot
* Additional snapshot options (overrides any project options):
  * widths - An array of widths to take screenshots at
  * minHeight - The minimum viewport height to take screenshots at
  * enableJavaScript - Enable JavaScript in Percy's rendering environment
  * percyCSS - Percy specific CSS only applied in Percy's rendering environment

## Global Configuration

You can also configure Percy to use the same options over all snapshots. To see supported configuration including widths read our [SDK configuration](https://docs.percy.io/docs/cli-configuration) docs

## Upgrading

### Automatically with `@percy/migrate`

We built a tool to help automate migrating to the new CLI toolchain! Migrating can be done by running the following commands and following the prompts:

```bash

$ npx @percy/migrate
? Are you currently using percy-java-selenium? Yes
? Install @percy/cli (required to run percy)? Yes
? Migrate Percy config file? Yes

```

This will automatically run the changes described below for you.

### Manually

#### Installing `@percy/cli` & `removing @percy/agent`

If you're coming from a pre-3.0 version of this package, make sure to install @percy/cli after upgrading to retain any existing scripts that reference the Percy CLI command. You will also want to uninstall `@percy/agent`, as it's been replaced by `@percy/cli`.

```bash

$ npm uninstall @percy/agent
$ npm install --save-dev @percy/cli

```
### Migrating Config

If you have a previous Percy configuration file, migrate it to the newest version with the [`config:migrate`](https://github.com/percy/cli/tree/master/packages/cli-config#percy-configmigrate-filepath-output) command:

```bash

$ npx percy config:migrate

```
