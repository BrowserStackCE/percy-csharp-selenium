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
        private static IWebDriver driver;
        private static Percy percy;
        private static TestServer server;

        public void StartAppAndOpenBrowser()
        {
            server = new TestServer();
            server.StartTestServer();
            // Create a headless Chrome browser.
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("--headless");
            driver = new ChromeDriver(options);
            percy = new Percy(driver);
        }

        public void closeBrowser()
        {
            // Close our test browser.
            driver.Quit();
            server.StopTestServer();
        }

        public void loadsHomePage()
        {
            driver.Navigate().GoToUrl(TEST_URL);
            IWebElement element = driver.FindElement(By.ClassName("todoapp"));
            Assert.IsNotNull(element);
            // Take a Percy snapshot.
            percy.Snapshot("Home Page");
        }

        public void acceptsANewTodo()
        {
            driver.Navigate().GoToUrl(TEST_URL);

            // We start with zero todos.
            var todoEls = driver.FindElements(By.CssSelector(".todo-list li"));
            Assert.AreEqual(0, todoEls.Count);
            // Add a todo in the browser.
            IWebElement newTodoEl = driver.FindElement(By.ClassName("new-todo"));
            newTodoEl.SendKeys("A new fancy todo!");
            newTodoEl.SendKeys(Keys.Return);

            // Now we should have 1 todo.
            todoEls = driver.FindElements(By.CssSelector(".todo-list li"));
            Assert.AreEqual(1, todoEls.Count);
            // Take a Percy snapshot specifying browser widths.
            percy.Snapshot("One todo", new List<int> { 768, 992, 1200 });
            driver.FindElement(By.ClassName("toggle")).Click();
            driver.FindElement(By.ClassName("clear-completed")).Click();
        }

        public void letsYouCheckOffATodo()
        {
            driver.Navigate().GoToUrl(TEST_URL);

            IWebElement newTodoEl = driver.FindElement(By.ClassName("new-todo"));
            newTodoEl.SendKeys("A new todo to check off");
            newTodoEl.SendKeys(Keys.Return);

            IWebElement todoCountEl = driver.FindElement(By.ClassName("todo-count"));
            Assert.AreEqual("1 item left", todoCountEl.Text);

            driver.FindElement(By.ClassName("toggle")).Click();

            todoCountEl = driver.FindElement(By.ClassName("todo-count"));
            Assert.AreEqual("0 items left", todoCountEl.Text);

            // Take a Percy snapshot specifying a minimum height.
            percy.Snapshot("Checked off todo", null, 2000);

        }

    }

}