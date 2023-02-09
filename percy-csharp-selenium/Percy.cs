using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using Newtonsoft.Json;

namespace percy_csharp_selenium
{
    /**
    * Percy client for visual testing.
    */
    public class Percy
    {
        // The JavaScript contained in dom.js
        private String _domJs = "";

        // Maybe get the CLI server address if not Set the CLI server address 
        //could be moved to percy-csharp-selenium Environment
        private String PERCY_SERVER_ADDRESS = System.Environment.GetEnvironmentVariable("PERCY_SERVER_ADDRESS") != null ?
                System.Environment.GetEnvironmentVariable("PERCY_SERVER_ADDRESS") : "http://localhost:5338";

        // Determine if we're debug logging
        private bool PERCY_DEBUG;

        // for logging
        private String LABEL;

        // Environment information like the programming language, browser, & SDK versions

        // Is the Percy server running or not
        private Boolean _isPercyEnabled;

        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private static readonly HttpClient _client = new HttpClient();

        /**
             * @param driver The Selenium WebDriver object that will hold the browser
             *               session to snapshot.
        */
        public Percy()
        {
            _isPercyEnabled = Healthcheck().Result;
            PERCY_DEBUG = System.Environment.GetEnvironmentVariable("PERCY_LOGLEVEL") != null &&
                System.Environment.GetEnvironmentVariable("PERCY_LOGLEVEL").Equals("debug");
            LABEL = "[\u001b[35m" + (PERCY_DEBUG ? "percy:cs" : "percy") + "\u001b[39m]";
        }

        /**
        * Checks to make sure the local Percy server is running. If not, disable Percy.
        */
        private async Task<Boolean> Healthcheck()
        {
            try
            {

                //Executing the Get request
                HttpResponseMessage response = await _client.GetAsync(PERCY_SERVER_ADDRESS + "/percy/healthcheck");
                int statusCode = (int)response.StatusCode;

                if (statusCode != 200)
                {
                    throw new Exception("Failed with HTTP error code : " + statusCode);
                }

                String version = null;
                IEnumerable<string> values;
                if (response.Headers.TryGetValues("x-percy-core-version", out values))
                {
                    //will return null if Header not found
                    version = values.FirstOrDefault();
                }

                if (version == null)
                {
                    Log("You may be using @percy/agent" +
                        "which is no longer supported by this SDK." +
                        "Please uninstall @percy/agent and install @percy/cli instead." +
                        "https://docs.percy.io/docs/migrating-to-percy-cli"
                        );

                    return false;
                }

                if (!version.Split('.')[0].Equals("1"))
                {
                    Log("Unsupported Percy CLI version, " + version);

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Percy is not running, disabling snapshots");
                if (PERCY_DEBUG)
                {
                    Log(ex.StackTrace);
                }

                return false;
            }

        }

        /**
             * Take a snapshot and upload it to Percy.
             *
             * @param name      The human-readable name of the snapshot. Should be unique.
             * @param options   A dictionary of key value pairs which specifies the params to be passed to Percy for generating screenshot. e.g. widths, minHeight, enableJavascript, percyCSS etc.
             */
        public void Snapshot(IWebDriver driver, String name, Dictionary<string, object> options)
        {
            if (!_isPercyEnabled)
            {
                return;
            }

            if (options == null)
            {
                options = new Dictionary<string, object>() { };
            }

            String domSnapshot = "";
            try
            {

                IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                jse.ExecuteScript(FetchPercyDOM().Result);
                bool enableJavaScript = false;
                if (options.ContainsKey("enableJavaScript"))
                {
                    enableJavaScript = (bool)options["enableJavaScript"];
                }
                domSnapshot = (String)jse.ExecuteScript(BuildSnapshotJS(enableJavaScript.ToString()));

            }
            catch (WebDriverException e)
            {
                // For some reason, the execution in the browser failed.
                if (PERCY_DEBUG)
                {
                    Log("Something went wrong attempting to take a snapshot:\n" + e.StackTrace);
                }
            }

            PostSnapshot(driver, domSnapshot, name, options);
        }

        /**
        * Attempts to load dom.js from the local Percy server. Use cached value in `domJs`,
        * if it exists.
        *
        * This JavaScript is critical for capturing snapshots. It serializes and captures
        * the DOM. Without it, snapshots cannot be captured.
        */
        private async Task<string> FetchPercyDOM()
        {

            if (!String.IsNullOrEmpty(_domJs.Trim()))
            {
                return _domJs;
            }

            try
            {
                HttpResponseMessage response = await _client.GetAsync(PERCY_SERVER_ADDRESS + "/percy/dom.js");
                int statusCode = (int)response.StatusCode;

                if (statusCode != 200)
                {
                    throw new Exception("Failed with HTTP error code: " + statusCode);
                }

                HttpContent httpEntity = response.Content;
                String domString = httpEntity.ReadAsStringAsync().Result;
                _domJs = domString;

                return domString;
            }
            catch (Exception ex)
            {
                _isPercyEnabled = false;
                if (PERCY_DEBUG)
                {
                    Log("Something went wrong attempting to fetch DOM:\n" + ex.StackTrace);
                }

            }

            return "";
        }

        /**
             * POST the DOM taken from the test browser to the Percy Agent node process.
             *
             * @param domSnapshot Stringified & serialized version of the site/applications DOM
             * @param name        The human-readable name of the snapshot. Should be unique.
             * @param options     A dictionary of key value pairs which specifies the params to be passed to Percy for generating screenshot. e.g. widths, minHeight, enableJavascript, percyCSS etc.
        */
        private void PostSnapshot(IWebDriver driver, String domSnapshot, String name, Dictionary<string, object> options)
        {
            if (!_isPercyEnabled)
            {
                return;
            }

            if (!options.ContainsKey("widths"))
            {
                options["widths"] = new List<int> { };
            }

            if (!options.ContainsKey("percyCSS"))
            {
                options["percyCSS"] = "";
            }

            if (!options.ContainsKey("enableJavaScript"))
            {
                options["enableJavaScript"] = false;
            }

            options["url"] = driver.Url;
            options["name"] = name;
            options["domSnapshot"] = domSnapshot;
            options["clientInfo"] = Environment.GetClientInfo();
            options["environmentInfo"] = Environment.GetEnvironmentInfo();
            
            var res = HttpPostPercySnapshot(options).Result;
        }

        private async Task<string> HttpPostPercySnapshot(Dictionary<string, object> options)
        {

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), PERCY_SERVER_ADDRESS + "/percy/snapshot"))
                    {
                        request.Content = new StringContent(JsonConvert.SerializeObject(options));
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                        var response = await httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var responseString = response.Content.ReadAsStringAsync().Result;
                    }
                }

            }
            catch (Exception ex)
            {
                Log("Could not post snapshot: " + options["name"]);
                if (PERCY_DEBUG)
                {
                    Log("An error occured when posting the snapshot:\n" + ex.StackTrace);
                }
            }

            return "";
        }


        /**
             * @return A String containing the JavaScript needed to instantiate a PercyAgent
             *         and take a snapshot.
        */
        private String BuildSnapshotJS(String enableJavaScript)
        {
            StringBuilder jsBuilder = new StringBuilder();
            // the double {{ and }} are needed to escape the curly braces
            jsBuilder.Append(String.Format("return PercyDOM.serialize({{ enableJavaScript: {0}, stringfy_response: true }})\n", enableJavaScript.ToLower()));
            return jsBuilder.ToString();
        }

        private void Log(String message)
        {
            Console.WriteLine(LABEL + " " + message);
        }

    }
}
