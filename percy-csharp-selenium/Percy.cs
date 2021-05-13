using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Net;
using System.Net.Http;
using System.Text;

using OpenQA.Selenium;

namespace percy_csharp_selenium
{
   /**
   * Percy client for visual testing.
   */
    public class Percy
    {
        // Selenium WebDriver we'll use for accessing the web pages to snapshot.
        private IWebDriver driver;

        // The JavaScript contained in dom.js
        private String domJs = "";

        // Maybe get the CLI server address if not Set the CLI server address 
        //could be moved to percy-csharp-selenium Environment
        private String PERCY_SERVER_ADDRESS = System.Environment.GetEnvironmentVariable("PERCY_SERVER_ADDRESS")!=null?
                System.Environment.GetEnvironmentVariable("PERCY_SERVER_ADDRESS") : "http://localhost:5338";

        // Environment information like the programming language, browser, & SDK versions
        private Environment env;

        // Is the Percy server running or not
        private Boolean isPercyEnabled = healthcheck();

        /**
             * @param driver The Selenium WebDriver object that will hold the browser
             *               session to snapshot.
        */
        public Percy(IWebDriver driver)
        {
            this.driver = driver; 
            this.env = new Environment(driver);
        }

        /**
             * Take a snapshot and upload it to Percy.
             *
             * @param name The human-readable name of the snapshot. Should be unique.
             *
        */
        public void Snapshot(String name)
        {
            Snapshot(name, null, 0, false, null);
        }

        /**
             * Take a snapshot and upload it to Percy.
             *
             * @param name   The human-readable name of the snapshot. Should be unique.
             * @param widths The browser widths at which you want to take the snapshot. In
             *               pixels.
        */
        public void Snapshot(String name, List<int> widths)
        {
            Snapshot(name, widths, 0, false, null);
        }

        /**
             * Take a snapshot and upload it to Percy.
             *
             * @param name   The human-readable name of the snapshot. Should be unique.
             * @param widths The browser widths at which you want to take the snapshot. In
             *               pixels.
             * @param minHeight The minimum height of the resulting snapshot. In pixels.
             */
        public void Snapshot(String name, List<int> widths, int minHeight)
        {
            Snapshot(name, widths, minHeight, false, null);
        }

        /**
             * Take a snapshot and upload it to Percy.
             *
             * @param name   The human-readable name of the snapshot. Should be unique.
             * @param widths The browser widths at which you want to take the snapshot. In
             *               pixels.
             * @param minHeight The minimum height of the resulting snapshot. In pixels.
             * @param enableJavaScript Enable JavaScript in the Percy rendering environment
             */
        public void Snapshot(String name, List<int> widths, int minHeight, bool enableJavaScript)
        {
            Snapshot(name, widths, minHeight, enableJavaScript, null);
        }

        /**
             * Take a snapshot and upload it to Percy.
             *
             * @param name      The human-readable name of the snapshot. Should be unique.
             * @param widths    The browser widths at which you want to take the snapshot.
             *                  In pixels.
             * @param minHeight The minimum height of the resulting snapshot. In pixels.
             * @param enableJavaScript Enable JavaScript in the Percy rendering environment
             * @param percyCSS Percy specific CSS that is only applied in Percy's browsers
             */
        public void Snapshot(String name, List<int> widths, int minHeight, bool enableJavaScript, String percyCSS)
        {
            if(!isPercyEnabled) {
                return;
            }

            String domSnapshot = "";
            try
            {
                IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                jse.ExecuteScript(fetchPercyDOM());
                domSnapshot = (String)jse.ExecuteScript(BuildSnapshotJS(enableJavaScript.ToString()));
            }
            catch (WebDriverException e)
            {
                // For some reason, the execution in the browser failed.
                Console.WriteLine("[percy] Something went wrong attempting to take a snapshot: " + e.Message);
            }

            PostSnapshot(domSnapshot, name, widths, minHeight, driver.Url, enableJavaScript, percyCSS);
        }

        /**
             * POST the DOM taken from the test browser to the Percy Agent node process.
             *
             * @param domSnapshot Stringified & serialized version of the site/applications DOM
             * @param name        The human-readable name of the snapshot. Should be unique.
             * @param widths      The browser widths at which you want to take the snapshot.
             *                    In pixels.
             * @param minHeight   The minimum height of the resulting snapshot. In pixels.
             * @param enableJavaScript Enable JavaScript in the Percy rendering environment
             * @param percyCSS Percy specific CSS that is only applied in Percy's browsers
        */
        private void PostSnapshot(String domSnapshot, String name, List<int> widths, int minHeight, String url, bool enableJavaScript, String percyCSS)
        {
            if (percyIsRunning == false)
            {
                return;
            }

            // Build a JSON object to POST back to the agent node process
            JsonObject json = new JsonObject();
            json.Add("url", url);
            json.Add("name", name);
            json.Add("percyCSS", percyCSS);
            json.Add("minHeight", minHeight);
            json.Add("domSnapshot", domSnapshot);
            json.Add("clientInfo", env.getClientInfo());
            //json.Add("clientInfo", "percy-java-selenium/unknown");
            json.Add("enableJavaScript", enableJavaScript);
            json.Add("environmentInfo", env.getEnvironmentInfo());
            //json.Add("environmentInfo", "selenium-java; MAC; chrome/85.0.4183.121");
            // Sending an empty array of widths to agent breaks asset discovery
            if (widths != null && widths.Count != 0)
            {
                json.Add("widths", JsonArray.Parse(GetSnapshotWidths(widths)));
            }

            string base_url = "http://localhost:5338/percy/snapshot";

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(base_url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = WebRequestMethods.Http.Post;
            httpWebRequest.ProtocolVersion = HttpVersion.Version11;
            try
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream(), Encoding.UTF8))
                {
                    streamWriter.Write(json.ToString());
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        // We don't really care about the response -- as long as their test suite doesn't fail
                        var result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[percy] An error occured when sending the DOM to agent: " + ex);
                percyIsRunning = false;
                Console.WriteLine("[percy] Percy has been disabled");
            }

        }

        private String GetSnapshotWidths(List<int> widths)
        {
            StringBuilder info = new StringBuilder();
            info.Append("[");
            string widthsStr = string.Join(",", widths);
            info.Append(widthsStr);
            info.Append("]");
            return info.ToString();
        }

        /**
             * @return A String containing the JavaScript needed to instantiate a PercyAgent
             *         and take a snapshot.
        */
        private String BuildSnapshotJS(String enableJavaScript)
        {
            StringBuilder jsBuilder = new StringBuilder();

            string test_script = "var percyAgentClient = new PercyAgent({ handleAgentCommunication: false })\nreturn percyAgentClient.snapshot('not used')";
            jsBuilder.Append(test_script);
            return jsBuilder.ToString();
        }
    }
}