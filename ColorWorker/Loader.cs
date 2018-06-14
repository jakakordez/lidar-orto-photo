using System;
using System.Drawing;
using System.IO;
using System.Net;
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

            lazReader.laszip_open_reader(filePath, ref compressed);
            var numberOfPoints = lazReader.header.number_of_point_records;
            //var kdTree = new KDTree(3);

            var img = GetOrthophotoImg();

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
            double minX = _bottomLeftX;
            double minY = _bottomLeftY;
            double maxX = minX + 999.999999999;
            double maxY = minY + 999.999999999;

            Console.WriteLine("[{0:hh:mm:ss}] Downloading image...", DateTime.Now);
            var request = WebRequest.Create("http://gis.arso.gov.si/arcgis/rest/services/DOF_2016/MapServer/export" +
                                               $"?bbox={minX}%2C{minY}%2C{maxX}%2C{maxY}&bboxSR=&layers=&layerDefs=" +
                                               $"&size={OrtoPhotoImgSize}%2C{OrtoPhotoImgSize}&imageSR=&format=png" +
                                               "&transparent=false&dpi=&time=&layerTimeOptions=" +
                                               "&dynamicLayers=&gdbVersion=&mapScale=&f=image");
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            //Console.WriteLine("[DONE]");
            return new Bitmap(responseStream ?? throw new Exception());
        }
    }
}
