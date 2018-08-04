using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OrtophotoDownloader
{
    class Program
    {
        static int Main(string[] args)
        {
            string path;
            int x1, y1, x2, y2;
            try
            {
                path = args[0];
                x1 = Convert.ToInt32(args[1]);
                x2 = Convert.ToInt32(args[2]);
                y1 = Convert.ToInt32(args[3]);
                y2 = Convert.ToInt32(args[4]);
            }
            catch
            {
                Console.WriteLine("Usage: OrtophotoDownloader PATH X Y");
                Console.WriteLine("Where:");
                Console.WriteLine("  PATH - repository path");
                Console.WriteLine("  X1 - x1 coordinate (374 - 624)");
                Console.WriteLine("  X2 - x2 coordinate (374 - 624)");
                Console.WriteLine("  y1 - y1 coordinate (30 - 194)");
                Console.WriteLine("  y2 - y2 coordinate (30 - 194)");
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
                for (int x = x1; x <= x2; x++)
                {
                    for (int y = y1; y <= y2; y++)
                    {
                        Console.WriteLine("[{0:hh:mm:ss}] Started downloading tile {1} {2} ", DateTime.Now, x, y);
                        Loader l = new Loader(x, y, path);
                        l.Start();
                    }
                }
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
