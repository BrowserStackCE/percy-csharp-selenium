using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;

namespace percy_csharp_selenium
{
    public class Environment
    {
        public Environment() { }

        private readonly static String DEFAULT_ARTIFACTID = "percy-csharp-selenium";
        private readonly static String UNKNOWN_VERSION = "unknown";
        private readonly IWebDriver driver;

        public Environment(IWebDriver driver)
        {
            this.driver = driver;
        }

        public String getClientInfo()
        {
            String artifactId = DEFAULT_ARTIFACTID;
            String version = UNKNOWN_VERSION;
            return String.Format("%s/%s", artifactId, version);
        }

        public String getEnvironmentInfo()
        {
            ICapabilities cap = ((IHasCapabilities)this.driver).Capabilities;

            String os;
            String browserName;
            String version;


            // check for platform
            if (cap.HasCapability("platformName"))
                os = cap.GetCapability("platformName").ToString().ToLower();
            else if (cap.HasCapability("platform"))
                os = cap.GetCapability("platform").ToString().ToLower();
            else if (cap.HasCapability("os"))
                os = cap.GetCapability("os").ToString().ToLower();
            else
                os = "unknownPlatform";


            // check for browser name
            if (cap.HasCapability("browserName"))
                browserName = cap.GetCapability("browserName").ToString().ToLower();
            if (cap.HasCapability("browser"))
                browserName = cap.GetCapability("browser").ToString().ToLower();
            else
                browserName = "unknownBrowser";


            // check for browser version
            if (cap.HasCapability("browserVersion"))
                version = cap.GetCapability("browserVersion").ToString().ToLower();
            else if (cap.HasCapability("version"))
                version = cap.GetCapability("version").ToString().ToLower();
            else
                version = "unknownVersion";


            return String.Format("selenium-csharp; %s; %s/%s", os, browserName, version);
        }
    }

}