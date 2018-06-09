using laszip.net;
using Newtonsoft.Json.Linq;
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
            Console.WriteLine("[{0:hh:mm:ss}] Reading and writing LAZ...", DateTime.Now);
            lazReader.laszip_seek_point(0L);//read from the beginning again

            lazWriter = new laszip_dll();
            lazWriter.header = lazReader.header;
            lazWriter.laszip_open_writer(ResourceDirectoryPath + "/4-" + x + "-" + y + ".laz", true);

            for (var pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
            {
                var coordArray = new double[3];
                lazReader.laszip_read_point();
                lazReader.laszip_get_coordinates(coordArray);
                lazWriter.point = lazReader.point;

                if(pointcloud.ContainsKey(pointIndex)
                    && lazWriter.point.classification == 2) lazWriter.point.classification = 9;

                lazWriter.laszip_write_point();
            }

            lazReader.laszip_close_reader();
            lazWriter.laszip_close_writer();
        }

        void LoadPointcloud()
        {
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
                    if (polygon.InPolygon(point))
                    {
                        pointcloud[pointIndex] = point;
                        break;
                    }
                }
            }
        }

        void LoadJson()
        {
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
