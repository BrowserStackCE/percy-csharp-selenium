﻿using System;
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
            String os = cap.GetCapability("platformName").ToString().ToLower();
            String browserName = cap.GetCapability("browserName").ToString().ToLower();
            String version = cap.GetCapability("browserVersion").ToString().ToLower();
            return String.Format("selenium-csharp; %s; %s/%s", os, browserName, version);
        }
    }

}