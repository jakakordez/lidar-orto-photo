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
            long points = 0;
            foreach (var file in new System.IO.DirectoryInfo(Environment.CurrentDirectory).GetFiles())
            {
                var lazReader = new laszip_dll();
                var compressed = true;
                var filePath = args.Length > 0 ? args[0] : "SloveniaLidarRGB0.laz"; //@"C:\Users\jakak\Desktop\nrg-sem-Potree\pointclouds\Normals\data\r\00000\r00000.laz";
                filePath = file.FullName;

                lazReader.laszip_open_reader(filePath, ref compressed);
                var numberOfPoints = lazReader.header.number_of_point_records;
                var coordArray = new double[3];
                points += numberOfPoints;
                Console.WriteLine("[{0:hh:mm:ss}] Reading LAZ... {1} points starting at {2}", DateTime.Now, numberOfPoints, lazReader.header.offset_to_point_data);
                
                for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
                {
                    lazReader.laszip_read_point();
                    lazReader.laszip_get_coordinates(coordArray);
                    var point = lazReader.laszip_get_point_pointer();
                    var colors = point.rgb;
                    Console.WriteLine(String.Format("X {0} Y {1} Z {2} R {3:X} G {4:X} B {5:X}", point.X, point.Y, point.Z, colors[0], colors[1], colors[2]));

                    if (pointIndex % 20 == 0 && Console.ReadKey().Key == ConsoleKey.Q) break;
                }
                Console.WriteLine("[DONE] ");
            }
            Console.WriteLine("Total: " + points);
            Console.Read();
        }
    }
}
