# percy-csharp-selenium

[Percy](https://percy.io) visual testing for C#.NET Selenium. (Supports both .NET Core and .NET Framework - 4.5 to 4.8)

## Resources

* [Example integration](https://github.com/samirans89/example-percy-csharp-selenium)

The project requires .NET Core 3.1 or higher. 

On Mac OS, you can use Homebrew:
```bash
$ brew install --cask dotnet
```

The Selenium tests use ChromeDriver, which you need to install separately for your system.

On Mac OS, you can use Homebrew:
```bash
$ brew tap homebrew/cask && brew install --cask chromedriver
```

On Windows, you can use Chocolatey:

```bash
$ choco install chromedriver
```

For other systems (or installation alternatives), see:
https://github.com/SeleniumHQ/selenium/wiki/ChromeDriver

## Building and running the app

You can use Node.js to build the code and trigger the test, simply run the below command. Ensure to supply a valid PERCY_TOKEN with the command.

```bash
$ npm install
$ PERCY_TOKEN=<YOUR_PROJECT_TOKEN> npm run test
```