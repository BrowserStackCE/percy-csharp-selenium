using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace percy_csharp_selenium
{

    /**
       * HTTP server that serves the static files that make our test app.
    */
    class TestServer
    {
        // Server port.
        private static readonly int PORT = 8000;

        // Location for static files in our test app.
        private static readonly String TESTAPP_DIR = "/testapp/";
        SimpleHTTPServer myServer = null;

        public void StartTestServer()
        {
            var work = new BlockingCollection<SimpleHTTPServer>();
            var producer1 = Task.Factory.StartNew(() => {
                work.TryAdd(myServer); // or whatever your threads are doing
            });

            var consumer = Task.Factory.StartNew(() => {
                foreach (var item in work.GetConsumingEnumerable())
                {
                    // do the work
                    //create server with given port
                    myServer = new SimpleHTTPServer(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + TESTAPP_DIR, PORT);
                    Console.WriteLine("Server is running on this port: " + myServer.Port.ToString());
                }
            });

        }

        public void StopTestServer()
        {

            var work = new BlockingCollection<SimpleHTTPServer>();
            var producer2 = Task.Factory.StartNew(() => {
                work.TryAdd(myServer); // or whatever your threads are doing
            });

            var consumer = Task.Factory.StartNew(() => {
                foreach (var item in work.GetConsumingEnumerable())
                {
                    // do the work
                    myServer.Stop();
                    Console.WriteLine("Server stopped on this port: " + myServer.Port.ToString());
                }
            });

        }

    }

}