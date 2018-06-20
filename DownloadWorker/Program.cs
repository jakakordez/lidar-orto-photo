using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DownloadWorker
{
    class Program
    {
        static int Main(string[] args)
        {
            string path;
            int x, y;
            try
            {
                path = args[0];
                x = Convert.ToInt32(args[1]);
                y = Convert.ToInt32(args[2]);
            }
            catch
            {
                Console.WriteLine("Usage: DownloadWorker PATH X Y");
                Console.WriteLine("Where:");
                Console.WriteLine("  PATH - repository path");
                Console.WriteLine("  X - x coordinate (374 - 624)");
                Console.WriteLine("  y - y coordinate (30 - 194)");
                Console.WriteLine("");
                Console.WriteLine("Received " + args.Length + " arguments:");
                foreach (var item in args)
                {
                    Console.WriteLine(item);
                }
                return 1;
            }
            try
            {
                Console.WriteLine("[{0:hh:mm:ss}] Start downloader tile {1} {2} ", DateTime.Now, x, y);
                Loader l = new Loader(x, y, path);
                l.Start();
            }
            catch (WebException e)
            {
                Console.WriteLine("Web exception ");
                if (e.Response is HttpWebRequest) Console.WriteLine("Code: " + ((HttpWebResponse)e.Response).StatusCode);
                Console.WriteLine(e.Response.ResponseUri.AbsoluteUri);
                var response = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
            }
            return 0;
        }
    }
}
