using laszip.net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAZReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var lazReader = new laszip_dll();
            var compressed = true;
            var filePath = args.Length > 0 ? args[0] : "SloveniaLidarRGB0.laz"; //@"C:\Users\jakak\Desktop\nrg-sem-Potree\pointclouds\Normals\data\r\00000\r00000.laz";

            lazReader.laszip_open_reader(filePath, ref compressed);
            var numberOfPoints = lazReader.header.number_of_point_records;
            var coordArray = new double[3];
            Console.WriteLine("[{0:hh:mm:ss}] Reading LAZ... {1} points", DateTime.Now, numberOfPoints);
            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                var point = lazReader.laszip_get_point_pointer();
                var colors = point.rgb;
                Console.WriteLine(String.Format("R {0:X} G {1:X} B {2:X}", colors[0], colors[1], colors[2]));

                if (pointIndex % 20 == 0 && Console.ReadKey().Key == ConsoleKey.Q) break;
            }
            Console.WriteLine("[DONE] ");
        }
    }
}
