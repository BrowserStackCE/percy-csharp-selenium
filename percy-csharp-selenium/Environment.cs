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

        public static String GetClientInfo()
        {
            String artifactID = DEFAULT_ARTIFACTID;
            String version = SDK_VERSION;
            return String.Format("{0}/{1}", artifactID, version);
        }

        public static String GetEnvironmentInfo()
        {
            return String.Format("selenium-csharp; {0}; {1}",OpenQA.Selenium.Internal.ResourceUtilities.AssemblyVersion, System.Environment.Version.ToString());
        }
    }

}