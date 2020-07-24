using laszip.net;
using Newtonsoft.Json.Linq;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WaterWorker
{
    class Loader
    {
        private string ResourceDirectoryPath;
        int x, y;
        Dictionary<int, XYZ> pointcloud;

        public Loader(int x, int y, string resourceDirectory)
        {
            this.x = x;
            this.y = y;
            ResourceDirectoryPath = resourceDirectory;
        }

        List<IPolygon> polygons;

        string sourcePath => ResourceDirectoryPath + "/3-" + x + "-" + y + ".laz";
        string destPath => ResourceDirectoryPath + "/4-" + x + "-" + y + ".laz";

        public void Start()
        {
            MapLayer mapLayer = new MapLayer(@".\hidrografija_D48");
            polygons = mapLayer.GetIPolygons(x, y);
            //LoadJson();
            if (polygons.Count == 0)
            {
                Console.WriteLine("[{0:hh:mm:ss}] No water on this tile", DateTime.Now);
                System.IO.File.Copy(sourcePath, destPath);
                return;
            }
            else Console.WriteLine("[{0:hh:mm:ss}] Total water points: {1}", DateTime.Now, polygons.Sum(p => p.ringPoints.Count()));
            LoadPointcloud();
            WritePointcloud();
        }
        laszip_dll lazReader, lazWriter;
        long numberOfPoints;

        void WritePointcloud()
        {
            Console.WriteLine("[{0:hh:mm:ss}] Generating water...", DateTime.Now);
            List<XYZ> newPoints = new List<XYZ>();
            foreach (var polygon in polygons)
            {
                var grid = polygon.GetGrid(1.0, x * 1000, y * 1000, (x + 1) * 1000, (y + 1) * 1000);
                newPoints.AddRange(grid);
            }

            Console.WriteLine("[{0:hh:mm:ss}] Writing LAZ...", DateTime.Now);
            lazReader.laszip_seek_point(0L);//read from the beginning again

            lazWriter = new laszip_dll();
            lazWriter.header = lazReader.header;

            lazWriter.header.number_of_point_records += (uint)newPoints.Count;
            lazWriter.laszip_open_writer(destPath, true);

            var coordArray = new double[3];
            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                lazWriter.point = lazReader.point;
                //if (lazWriter.point.classification > 9) System.Diagnostics.Debugger.Break();
                if(pointcloud.ContainsKey(pointIndex)) lazWriter.point.classification = WATER_CLASSIFICATION;

                lazWriter.laszip_write_point();
            }

            Console.WriteLine("[{0:hh:mm:ss}] Adding water...", DateTime.Now);
            foreach (var gridPoint in newPoints)
            {
                lazWriter.point.classification = 9;
                lazWriter.point.point_source_ID = 1234;
                coordArray[0] = gridPoint.x;
                coordArray[1] = gridPoint.y;
                coordArray[2] = gridPoint.z;
                lazWriter.laszip_set_coordinates(coordArray);
                lazWriter.laszip_write_point();
            }
            lazReader.laszip_close_reader();
            lazWriter.laszip_close_writer();
        }

        public static Func<double[], double[], double> L2Norm = (x, y) =>
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }

            return dist;
        };

        public const int GROUND_CLASSIFICATION = 2;
        public const int WATER_CLASSIFICATION = 9;
        void LoadPointcloud()
        {
            Console.WriteLine("[{0:hh:mm:ss}] Reading LAZ...", DateTime.Now);
            pointcloud = new Dictionary<int, XYZ>();
            lazReader = new laszip_dll();
            var compressed = true;

            lazReader.laszip_open_reader(sourcePath, ref compressed);
            numberOfPoints = lazReader.header.number_of_point_records;

            Console.WriteLine("[{0:hh:mm:ss}] Total: {1} mio points", DateTime.Now, Math.Round(numberOfPoints/1000000.0, 2));

            /*foreach (var item in polygons)
            {
                Console.WriteLine(GetGeogebraString(item.ringPoints.ToList()));
            }*/

            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                if(pointIndex%1000000 == 0 && pointIndex > 0)
                    Console.WriteLine("[{0:hh:mm:ss}] {1} mio points processed", DateTime.Now, pointIndex/1000000);
                var coordArray = new double[3];
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                XYZ point = new XYZ() { x = coordArray[0], y = coordArray[1], z = coordArray[2] };
                foreach (var polygon in polygons)
                {
                    if (polygon.InPolygon(point) || polygon.DistanceFromCoast(point) < 5) {
                        if (lazReader.point.classification == WATER_CLASSIFICATION) {
                            polygon.points[pointIndex] = point;
                            pointcloud[pointIndex] = point;
                        }
                        if(lazReader.point.classification == GROUND_CLASSIFICATION
                            || lazReader.point.classification == WATER_CLASSIFICATION)
                            polygon.allPoints[pointIndex] = point;
                        break;
                    }
                }
            }
            /*
            foreach (var item in polygons)
            {
                Console.WriteLine(GetGeogebraString(Subsample(item.allPoints.Values.ToList(), 10)));
            }*/

            foreach (var polygon in polygons.Where(p => p.allPoints.Count > 0))
            {
                //var treeData = polygon.allPoints.Values.Select(p => new double[] { p.x, p.y }).ToArray();
                //KDTree<double, XYZ> tree = new KDTree<double, XYZ>(2, treeData, polygon.allPoints.Values.ToArray(), L2Norm);
                polygon.AssignHeghts();
                /*double elevation;
                if (polygon.points.Count > 0) elevation = polygon.points.Select(p => p.Value.z).Average();
                else elevation = polygon.allPoints.Select(p => p.Value.z).Min();*/

                foreach (var point in polygon.allPoints)
                {
                    if (!pointcloud.ContainsKey(point.Key) /*&&
                        polygon.IsOnWater(point.Value, 15)*/)
                    {
                        /*var nearestPoints = tree.NearestNeighbors(
                            new double[] { point.Value.x, point.Value.y }, 100);
                        var z = nearestPoints.Min(p => p.Item2.z);
                        */
                        var nearest = polygon.ringsTree.NearestNeighbors(new double[] { point.Value.x, point.Value.y}, 3);
                        var elevation = nearest.Select(p => p.Item2.z).Average();
                        if (point.Value.z < elevation + 2.0
                            && polygon.IsOnWater(point.Value, 15))
                        {
                            polygon.points[point.Key] = point.Value;
                            pointcloud[point.Key] = point.Value;
                        }
                    }
                }
            }
            var ringPoints = polygons
                .Where(p => p.allPoints.Count > 0)
                .SelectMany(p => p.ringPoints);
            if (ringPoints.Count() > 0)
            {
                foreach (var polygon in polygons.Where(p => p.allPoints.Count == 0))
                {
                    polygon.AssignHeghts(ringPoints);
                }
            }
            else
            {
                //throw new Exception("No points to determine ring heights!");
                Console.WriteLine("No points to determine ring heights");
                System.IO.File.Copy(sourcePath, destPath);
                System.Environment.Exit(0);
            }
        }

        public static IEnumerable<XYZ> Subsample(IList<XYZ> points, int n)
        {
            return points
                .Select((a, b) => new KeyValuePair<int, XYZ>(b, a))
                .Where(a => a.Key % n == 0)
                .Select(a => a.Value);
        }

        public static string GetGeogebraString(IEnumerable<XYZ> points)
        {
            StringBuilder b = new StringBuilder();
            b.Append("{");
            bool first = true;
            foreach (var item in points)
            {
                if (!first) b.Append(",");
                first = false;
                b.AppendFormat(CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0})", item.x, item.y);
            }
            b.Append("}");
            return b.ToString();
        }

        void LoadJson()
        {
            Console.WriteLine("[{0:hh:mm:ss}] Loading JSON", DateTime.Now);
            polygons = new List<IPolygon>();
            string url = BuildUrl(x * 1000, y * 1000, (x + 1) * 1000, (y + 1) * 1000);
            WebClient client = new WebClient();
            var data = Encoding.UTF8.GetString(client.DownloadData(url));
            JObject jsonObject = JObject.Parse(data);
            foreach (var entity in jsonObject.Last.First.Children())
            {
                polygons.Add(new Polygon(entity));
            }
        }

        static string BuildUrl(int xmin, int ymin, int xmax, int ymax)
        {
            StringBuilder b = new StringBuilder();
            b.Append("http://gis.arso.gov.si/arcgis/rest/services/Topografske_karte_ARSO_nova/MapServer/12/query?");
            b.Append("where=TIP_SV+%3E+2+or+TIP_SV+%3C+2&");
            b.Append("text=&");
            b.Append("objectIds=&");
            b.Append("time=&");
            b.Append("geometry=%7Bxmin%3A+" + xmin + "%2C+ymin%3A+" + ymin + "%2C+xmax%3A+" + xmax + "%2C+ymax%3A+" + ymax + "%7D+&");
            b.Append("geometryType=esriGeometryEnvelope&");
            b.Append("inSR=&");
            b.Append("spatialRel=esriSpatialRelIntersects&");
            b.Append("relationParam=&");
            b.Append("outFields=GEOG_IME%2C+OBJECTID%2C+VRSTA_O%2C+TIP_PREH_O%2C+IZVOR_O%2C+STALN_O&");
            b.Append("returnGeometry=true&");
            b.Append("returnTrueCurves=false&");
            b.Append("maxAllowableOffset=&");
            b.Append("geometryPrecision=&");
            b.Append("outSR=&");
            b.Append("returnIdsOnly=false&");
            b.Append("returnCountOnly=false&");
            b.Append("orderByFields=&");
            b.Append("groupByFieldsForStatistics=&");
            b.Append("outStatistics=&");
            b.Append("returnZ=false&");
            b.Append("returnM=false&");
            b.Append("gdbVersion=&");
            b.Append("returnDistinctValues=false&");
            b.Append("resultOffset=&");
            b.Append("resultRecordCount=&");
            b.Append("queryByDistance=&");
            b.Append("returnExtentsOnly=false&");
            b.Append("datumTransformation=&");
            b.Append("parameterValues=&");
            b.Append("rangeValues=&");
            b.Append("f=pjson");
            return b.ToString();
        }
    }
}
