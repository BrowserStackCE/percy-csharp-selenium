using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;

namespace percy_csharp_selenium
{
    public class Environment
    {
        public Environment() { }

        private readonly static String DEFAULT_ARTIFACTID = "percy-csharp-selenium";
        private readonly static String SDK_VERSION = "1.0.0";
        private readonly IWebDriver _driver;

        public Environment(IWebDriver _driver)
        {
            this._driver = _driver;
        }

        public String GetClientInfo()
        {
            String artifactID = DEFAULT_ARTIFACTID;
            String version = SDK_VERSION;
            return String.Format("{0}/{1}", artifactID, version);
        }

        public String GetEnvironmentInfo()
        {
            ICapabilities capabilities = ((IHasCapabilities)this._driver).Capabilities;

            String os;
            String browserName;
            String version;


            // check for platform
            if (capabilities.HasCapability("platformName"))
                os = capabilities.GetCapability("platformName").ToString().ToLower();
            else if (capabilities.HasCapability("platform"))
                os = capabilities.GetCapability("platform").ToString().ToLower();
            else if (capabilities.HasCapability("os"))
                os = capabilities.GetCapability("os").ToString().ToLower();
            else
                os = "unknownPlatform";


            // check for browser name
            if (capabilities.HasCapability("browserName"))
                browserName = capabilities.GetCapability("browserName").ToString().ToLower();
            if (capabilities.HasCapability("browser"))
                browserName = capabilities.GetCapability("browser").ToString().ToLower();
            else
                browserName = "unknownBrowser";


            // check for browser version
            if (capabilities.HasCapability("browserVersion"))
                version = capabilities.GetCapability("browserVersion").ToString().ToLower();
            else if (capabilities.HasCapability("version"))
                version = capabilities.GetCapability("version").ToString().ToLower();
            else
                version = "unknownVersion";


            return String.Format("selenium-csharp; {0}; {1}/{2}", os, browserName, version);
        }
    }

}