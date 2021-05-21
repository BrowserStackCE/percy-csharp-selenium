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
        private Boolean isPercyEnabled;

        // HttpClient is intended to be instantiated once per application, rather than per-use.
        private static readonly HttpClient client = new HttpClient();

        /**
             * @param driver The Selenium WebDriver object that will hold the browser
             *               session to snapshot.
        */
        public Percy(IWebDriver driver)
        {
            this.driver = driver; 
            this.env = new Environment(driver);
            isPercyEnabled = Healthcheck().Result;
        }

        /**
        * Checks to make sure the local Percy server is running. If not, disable Percy.
        */
        private async Task<Boolean> Healthcheck() {
            try {
                
                //Executing the Get request
                HttpResponseMessage response = await client.GetAsync(PERCY_SERVER_ADDRESS + "/percy/healthcheck");
                int statusCode = (int)response.StatusCode;

                if (statusCode != 200){
                    throw new Exception("Failed with HTTP error code : " + statusCode);
                }

                String version = null;
                IEnumerable<string> values;
                if (response.Headers.TryGetValues("x-percy-core-version", out values) ) {
                    //will return null if Header not found
                    version = values.FirstOrDefault();
                }

                if (version == null) {
                    Console.WriteLine("You may be using @percy/agent" +
                        "which is no longer supported by this SDK." +
                        "Please uninstall @percy/agent and install @percy/cli instead." +
                        "https://docs.percy.io/docs/migrating-to-percy-cli"
                        );

                    return false;
                }

                if (!version.Split('.')[0].Equals("1")) {
                    Console.WriteLine("Unsupported Percy CLI version, " + version);

                    return false;
                }

                return true;
            } catch (Exception ex) {
                Console.WriteLine("\nException Caught!");	
                Console.WriteLine("Message :{0} ",ex.Message);

                return false;
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
            if(!isPercyEnabled) {
                return;
            }

            String domSnapshot = "";
            try
            {
                IJavaScriptExecutor jse = (IJavaScriptExecutor)driver;
                jse.ExecuteScript(FetchPercyDOM().Result);
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
        * Attempts to load dom.js from the local Percy server. Use cached value in `domJs`,
        * if it exists.
        *
        * This JavaScript is critical for capturing snapshots. It serializes and captures
        * the DOM. Without it, snapshots cannot be captured.
        */
        private async Task<string> FetchPercyDOM() {
        
            try {
                HttpResponseMessage response = await client.GetAsync(PERCY_SERVER_ADDRESS + "/percy/dom.js");
                int statusCode = (int)response.StatusCode;

                if (statusCode != 200){
                    throw new Exception("Failed with HTTP error code: " + statusCode);
                }


                HttpContent httpEntity = response.Content;
                String domString = httpEntity.ReadAsStringAsync().Result;
                domJs = domString;

                return domString;
            } catch (Exception ex) {
                isPercyEnabled = false;
                Console.WriteLine("[percy] Something went wrong attempting to fetch DOM: " + ex.Message);

            }

            return "";
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
        private  void PostSnapshot(String domSnapshot, String name, List<int> widths, int minHeight, String url, bool enableJavaScript, String percyCSS)
        {
            if (!isPercyEnabled)
            {
                return;
            }

            if (widths == null)
            {
                widths =  new List<int>{};
            }

            if (percyCSS == null)
            {
                percyCSS = "";
            }

            var  param_dict = new Dictionary<string, object>(){

                {"url", url},
                {"name", name},
                {"percyCSS", percyCSS},
                {"domSnapshot", domSnapshot },
                {"clientInfo", env.getClientInfo()},
                { "enableJavaScript", enableJavaScript },
                { "environmentInfo", env.getEnvironmentInfo()},
                { "widths", widths},
                { "minHeight", minHeight }

            };


            var res = HttpPostPercySnapshot(param_dict).Result;



        }

        private async  Task<string> HttpPostPercySnapshot(Dictionary<string, object> param_dict)
        {

            try
            {  
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), PERCY_SERVER_ADDRESS + "/percy/snapshot"))
                    {
                        
                        request.Content = new StringContent(JsonConvert.SerializeObject(param_dict));
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                        var response = await httpClient.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var responseString = response.Content.ReadAsStringAsync().Result;

                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("[percy] could not post snapshot: " + param_dict["name"]);
                Console.WriteLine("[percy] An error occured when posting the snapshot: " + ex);
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
            jsBuilder.Append(String.Format("return PercyDOM.serialize({{ enableJavaScript: {0} }})\n", enableJavaScript.ToLower()));
            return jsBuilder.ToString();
        }
    }
}