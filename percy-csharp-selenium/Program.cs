using System;

namespace percy_csharp_selenium
{
    class Program
    {
        static void Main(string[] args)
        {
            AppTest test_object = new AppTest();
            test_object.StartAppAndOpenBrowser();
            test_object.LoadsHomePage();
            test_object.AcceptsANewTodo();
            test_object.LetsYouCheckOffATodo();
            test_object.CloseBrowser();
        }
    }
}