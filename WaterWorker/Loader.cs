using laszip.net;
using Newtonsoft.Json.Linq;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
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

        List<Polygon> polygons;

        public void Start()
        {
            LoadJson();
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
            lazWriter.laszip_open_writer(ResourceDirectoryPath + "/4-" + x + "-" + y + ".laz", true);

            var coordArray = new double[3];
            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                lazWriter.point = lazReader.point;

                if(pointcloud.ContainsKey(pointIndex)) lazWriter.point.classification = WATER_CLASSIFICATION;

                lazWriter.laszip_write_point();
            }

            Console.WriteLine("[{0:hh:mm:ss}] Adding water...", DateTime.Now);
            foreach (var gridPoint in newPoints)
            {
                lazWriter.point.classification = 9;
                coordArray[0] = gridPoint.x;
                coordArray[1] = gridPoint.y;
                coordArray[2] = gridPoint.z;
                lazWriter.laszip_set_coordinates(coordArray);
                lazWriter.laszip_write_point();
            }
            lazReader.laszip_close_reader();
            lazWriter.laszip_close_writer();
        }

        KDTree<double, XYZ> tree;
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
            var filePath = ResourceDirectoryPath + "/3-" + x + "-" + y + ".laz";

            lazReader.laszip_open_reader(filePath, ref compressed);
            numberOfPoints = lazReader.header.number_of_point_records;

            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                var coordArray = new double[3];
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                XYZ point = new XYZ() { x = coordArray[0], y = coordArray[1], z = coordArray[2] };
                foreach (var polygon in polygons)
                {
                    if (polygon.InPolygon(point)) {
                        if (lazReader.point.classification == WATER_CLASSIFICATION) {
                            polygon.points[pointIndex] = point;
                            pointcloud[pointIndex] = point;
                        }
                        polygon.allPoints[pointIndex] = point;
                        break;
                    }
                }
            }

            foreach (var polygon in polygons)
            {
                if (polygon.allPoints.Count == 0) continue;
                //var treeData = polygon.allPoints.Values.Select(p => new double[] { p.x, p.y }).ToArray();
                //KDTree<double, XYZ> tree = new KDTree<double, XYZ>(2, treeData, polygon.allPoints.Values.ToArray(), L2Norm);
                double elevation;
                if (polygon.points.Count > 0) elevation = polygon.points.Select(p => p.Value.z).Average();
                else elevation = polygon.allPoints.Select(p => p.Value.z).Min();

                foreach (var point in polygon.allPoints)
                {
                    if (!pointcloud.ContainsKey(point.Key) /*&&
                        polygon.IsOnWater(point.Value, 15)*/)
                    {

                        /*var nearestPoints = tree.NearestNeighbors(
                            new double[] { point.Value.x, point.Value.y }, 100);
                        var z = nearestPoints.Min(p => p.Item2.z);
                        */
                        if (point.Value.z < elevation + 1.0)
                        {
                            polygon.points[point.Key] = point.Value;
                            pointcloud[point.Key] = point.Value;
                        }
                    }
                }
            }
        }

        void LoadJson()
        {
            Console.WriteLine("[{0:hh:mm:ss}] Loading JSON", DateTime.Now);
            polygons = new List<Polygon>();
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
