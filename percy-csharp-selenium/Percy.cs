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

        // We'll expect this file to exist at the root of our classpath, as a resource.
        private static readonly String AGENTJS_FILE = "percy-agent.js";

        // Selenium WebDriver we'll use for accessing the web pages to snapshot.
        private IWebDriver driver;

        // The JavaScript contained in percy-agent.js
        private String percyAgentJs;

        // Environment information like the programming language, browser, & SDK versions
        private Environment env;

        // Is the Percy Agent process running or not
        private bool percyIsRunning = true;

        /**
             * @param driver The Selenium WebDriver object that will hold the browser
             *               session to snapshot.
        */
        public Percy(IWebDriver driver)
        {
            this.driver = driver;
            this.env = new Environment(driver);
            this.percyAgentJs = LoadPercyAgentJsAsync().Result;
        }

        /**
             * Attempts to load percy-agent.js from `http://localhost:5338/percy-agent.js`.
             *
             * This JavaScript is critical for capturing snapshots. It serializes and captures
             * the DOM. Without it, snapshots cannot be captured.
        */
        private async System.Threading.Tasks.Task<string> LoadPercyAgentJsAsync()
        {
            try
            {
                //Creating a HttpGet object
                using HttpClient client = new HttpClient();
                var response = await client.GetAsync("http://localhost:5338/" + AGENTJS_FILE);

                int statusCode = (int)response.StatusCode;
                if (statusCode != 200)
                {
                    throw new Exception("Failed with HTTP error code : " + statusCode);
                }

                String agentJSString = await response.Content.ReadAsStringAsync();
                return agentJSString;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[percy] An error occured while retrieving percy-agent.js: " + ex);
                percyIsRunning = false;
                Console.WriteLine("[percy] Percy has been disabled");
                return null;
            }
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
            String domSnapshot = "";

            if (percyAgentJs == null)
            {
                // This would happen if we couldn't load percy-agent.js in the constructor.
                return;
            }

            string script = null;
            try
            {
                IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                jse.ExecuteScript(percyAgentJs);
                script = BuildSnapshotJS();
                domSnapshot = (String)jse.ExecuteScript(script);
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
        private String BuildSnapshotJS()
        {
            StringBuilder jsBuilder = new StringBuilder();

            string test_script = "var percyAgentClient = new PercyAgent({ handleAgentCommunication: false })\nreturn percyAgentClient.snapshot('not used')";
            jsBuilder.Append(test_script);
            return jsBuilder.ToString();
        }
    }
}