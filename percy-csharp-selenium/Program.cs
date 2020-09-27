using System;

namespace percy_csharp_selenium
{
    class Program
    {
        static void Main(string[] args)
        {
            AppTest test_object = new AppTest();
            test_object.StartAppAndOpenBrowser();
            test_object.loadsHomePage();
            test_object.acceptsANewTodo();
            test_object.letsYouCheckOffATodo();
            test_object.closeBrowser();
        }
    }
}