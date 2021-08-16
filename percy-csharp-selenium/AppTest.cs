using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;

namespace percy_csharp_selenium
{

   /**
   * Unit test for example App.
   */
    public class AppTest
    {
        private static readonly String TEST_URL = "http://localhost:8000";
        private static IWebDriver _driver;
        private static Percy _percy;
        private static TestServer _server;

        public void StartAppAndOpenBrowser()
        {
            _server = new TestServer();
            _server.StartTestServer();
            // Create a headless Chrome browser.
            ChromeOptions Options = new ChromeOptions();
            Options.AddArguments("--headless");
            _driver = new ChromeDriver(Options);
            _percy = new Percy();
        }

        public void CloseBrowser()
        {
            // Close our test browser.
            _driver.Quit();
            _server.StopTestServer();
        }

        public void LoadsHomePage()
        {
            _driver.Navigate().GoToUrl(TEST_URL);
            IWebElement Element = _driver.FindElement(By.ClassName("todoapp"));
            Assert.IsNotNull(Element);
            // Take a Percy snapshot.
            _percy.Snapshot(_driver,"Home Page", null);
        }

        public void AcceptsANewTodo()
        {
            _driver.Navigate().GoToUrl(TEST_URL);

            // We start with zero todos.
            var toDoEls = _driver.FindElements(By.CssSelector(".todo-list li"));
            Assert.AreEqual(0, toDoEls.Count);
            // Add a todo in the browser.
            IWebElement newToDoEl = _driver.FindElement(By.ClassName("new-todo"));
            newToDoEl.SendKeys("A new fancy todo!");
            newToDoEl.SendKeys(Keys.Return);

            // Now we should have 1 todo.
            toDoEls = _driver.FindElements(By.CssSelector(".todo-list li"));
            Assert.AreEqual(1, toDoEls.Count);
            // Take a Percy snapshot specifying browser widths.
            _percy.Snapshot(_driver,"One todo", new Dictionary<string, object> { { "widths", new List<int> { 768, 992, 1200 } } });
            _driver.FindElement(By.ClassName("toggle")).Click();
            _driver.FindElement(By.ClassName("clear-completed")).Click();
        }

        public void LetsYouCheckOffATodo()
        {
            _driver.Navigate().GoToUrl(TEST_URL);

            IWebElement newToDoEl = _driver.FindElement(By.ClassName("new-todo"));
            newToDoEl.SendKeys("A new todo to check off");
            newToDoEl.SendKeys(Keys.Return);

            IWebElement toDoCountEl = _driver.FindElement(By.ClassName("todo-count"));
            Assert.AreEqual("1 item left", toDoCountEl.Text);

            _driver.FindElement(By.ClassName("toggle")).Click();

            toDoCountEl = _driver.FindElement(By.ClassName("todo-count"));
            Assert.AreEqual("0 items left", toDoCountEl.Text);

            // Take a Percy snapshot specifying a minimum height.
            _percy.Snapshot(_driver, "Checked off todo", new Dictionary<string, object> { { "minHeight", 2000 } });

        }

    }

}