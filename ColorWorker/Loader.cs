﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using laszip.net;
using Point = System.Windows.Point;

namespace ColorWorker
{
    class Loader
    {
        private string ResourceDirectoryPath;
        int x, y;
        private const int OrtoPhotoImgSize = 2000;
        private int _bottomLeftX;
        private int _bottomLeftY;

        public Loader(int x, int y, string resourceDirectory)
        {
            this.x = x;
            this.y = y;
            ResourceDirectoryPath = resourceDirectory;
            _bottomLeftX = x * 1000;
            _bottomLeftY = y * 1000;
        }

        public void Start()
        {
            var lazReader = new laszip_dll();
            var compressed = true;
            var filePath = ResourceDirectoryPath + "/5-" + x + "-" + y + ".laz";

            
            //var kdTree = new KDTree(3);
            Bitmap img = null;
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    img = GetOrthophotoImg();
                    break;
                }
                catch (Exception e){
                    Console.WriteLine("[{0:hh:mm:ss}] Image download failed...", DateTime.Now);
                    Console.WriteLine("[{0:hh:mm:ss}] {1}", DateTime.Now, e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            if (img == null) throw new Exception();

            lazReader.laszip_open_reader(filePath, ref compressed);
            var numberOfPoints = lazReader.header.number_of_point_records;

            Console.WriteLine("[{0:hh:mm:ss}] Reading and writing LAZ...", DateTime.Now);
            lazReader.laszip_seek_point(0L);//read from the beginning again
            lazReader.laszip_open_reader(filePath, ref compressed);

            var lazWriter = new laszip_dll();
            lazWriter.header = lazReader.header;
            lazWriter.laszip_open_writer(ResourceDirectoryPath + "/6-" + x + "-" + y + ".laz", true);

            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                var coordArray = new double[3];
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                lazWriter.point = lazReader.point;

                int[] pxCoordinates = FindClosestPxCoordinates(coordArray[0], coordArray[1]);
                int i = (pxCoordinates[0] - _bottomLeftX) * 2;
                int j = img.Height - 1 - ((pxCoordinates[1] - _bottomLeftY) * 2);//j index of image goes from top to bottom

                i = Math.Max(Math.Min(i, img.Width - 1), 0);
                j = Math.Max(Math.Min(j, img.Height - 1), 0);

                Color color = img.GetPixel(i, j); //binary int value						
                lazReader.point.rgb = new[] {
                    (ushort) (color.R << 8),
                    (ushort) (color.G << 8),
                    (ushort) (color.B << 8),
                    (ushort) 0
                };
                lazWriter.laszip_write_point();
            }
            lazReader.laszip_close_reader();
            lazWriter.laszip_close_writer();
        }

        private int[] FindClosestPxCoordinates(double x, double y)
        {

            var decimalPartX = x - Math.Floor(x);
            var decimalPartY = y - Math.Floor(y);
            x = decimalPartX >= 0 && decimalPartX < 0.5 ? (int)x : (int)x + 0.5; //0.0...0.49 -> 0.0	    
            y = decimalPartY >= 0 && decimalPartY < 0.5 ? (int)y : (int)y + 0.5; //0.5...0.99 -> 0.5
            
            var p = new Point(x, y);
            var upperLeft = new Point((int)x, (int)y + 0.5);
            var upperRight = new Point((int)x + 0.5, (int)y + 0.5);
            var bottomLeft = new Point((int)x, (int)y);
            var bottomRight = new Point((int)x + 0.5, (int)y);

            //leftBottom is never out of bounds
            var closestPoint = bottomLeft;
            var minDistance = (p - bottomLeft).Length;

            var points = new[] { upperLeft, upperRight, bottomRight };
            foreach (var currPoint in points)
            {
                if (IsPointOutOfBounds(currPoint)) continue;
                var currDistance = (p - currPoint).Length;
                if (currDistance < minDistance)
                {
                    closestPoint = currPoint;
                    minDistance = currDistance;
                }

            }
            return new[] { (int)closestPoint.X, (int)closestPoint.Y };
        }

        //p is out of bounds if x or y coordinate is bigger than width of length of image 
        private bool IsPointOutOfBounds(Point p)
        {
            double maxX = _bottomLeftX + (OrtoPhotoImgSize - 1);
            double maxY = _bottomLeftY + (OrtoPhotoImgSize - 1);

            return p.X > maxX || p.Y > maxY;
        }

        //download and return Image created based on bounds -> _bottomLeftX, _bottomLeftY
        public Bitmap GetOrthophotoImg()
        {
            string filename = x + "-" + y + ".png";
            if (File.Exists(filename))
            {
                Console.WriteLine("[{0:hh:mm:ss}] Found local image...", DateTime.Now);
                return new Bitmap(filename);
            }
            else Console.WriteLine("[{0:hh:mm:ss}] Image with filename " + filename + " not found", DateTime.Now);

            double minX = _bottomLeftX;
            double minY = _bottomLeftY;
            double maxX = minX + 999.999999999;
            double maxY = minY + 999.999999999;

            Console.WriteLine("[{0:hh:mm:ss}] Downloading image with web client...", DateTime.Now);

            /*string remoteUri = "http://gis.arso.gov.si/arcgis/rest/services/DOF_2016/MapServer/export" +
                                                $"?bbox={minX}%2C{minY}%2C{maxX}%2C{maxY}&bboxSR=&layers=&layerDefs=" +
                                                $"&size={OrtoPhotoImgSize}%2C{OrtoPhotoImgSize}&imageSR=&format=png" +
                                                "&transparent=false&dpi=&time=&layerTimeOptions=" +
                                                "&dynamicLayers=&gdbVersion=&mapScale=&f=image";
            string fileName = minX+"-"+minY+".png ";

            WebClient myWebClient = new WebClient();
            myWebClient.DownloadFile(remoteUri, fileName);

            return (Bitmap)Image.FromFile(fileName);*/

            /*var request = WebRequest.CreateHttp("http://gis.arso.gov.si/arcgis/rest/services/DOF_2016/MapServer/export" +
                                                $"?bbox={minX}%2C{minY}%2C{maxX}%2C{maxY}&bboxSR=&layers=&layerDefs=" +
                                                $"&size={OrtoPhotoImgSize}%2C{OrtoPhotoImgSize}&imageSR=&format=png" +
                                                "&transparent=false&dpi=&time=&layerTimeOptions=" +
                                                "&dynamicLayers=&gdbVersion=&mapScale=&f=image");
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();*/

            var sb = new StringBuilder();
            sb.Append("./Python/map_exporter.py --bbox-format=gk -o ");
            sb.Append(filename);
            sb.Append(" -f png --width 2000 --height 2000 ");
            sb.Append(5000000 + minY);
            sb.Append(" ");
            sb.Append(5000000 + minX);
            sb.Append(" ");
            sb.Append(5000000 + maxY);
            sb.Append(" ");
            sb.Append(5000000 + maxX);
            sb.Append(" --map-dir=Maps --map-conf ./Python/dof_2016.json --verbose");
            
            Process process = new Process();
            var username = Environment.UserName;
            process.StartInfo.FileName = @"C:\Users\"+username+@"\Miniconda3\python.exe";
            Console.WriteLine("Python interpreter: " + process.StartInfo.FileName);
            process.StartInfo.Arguments = sb.ToString();
            Console.WriteLine("Command: " + sb.ToString());
            process.StartInfo.WorkingDirectory = System.Environment.CurrentDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            while (!process.WaitForExit(1))
            {

            }
            //Console.WriteLine(process.ExitCode);
            //Console.WriteLine(process.StandardOutput.ReadToEnd());
            //Console.WriteLine("[DONE]");
            //return new Bitmap(responseStream?? throw new Exception());
            return new Bitmap(Bitmap.FromFile(filename));
        }
    }
}
