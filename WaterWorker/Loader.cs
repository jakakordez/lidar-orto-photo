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

        public Loader(int x, int y, string resourceDirectory)
        {
            this.x = x;
            this.y = y;
            ResourceDirectoryPath = resourceDirectory;
        }

        public void Start()
        {
            string url = BuildUrl(x * 1000, y * 1000, (x + 1) * 1000, (y + 1) * 1000);
            WebClient client = new WebClient();
            var data = Encoding.UTF8.GetString(client.DownloadData(url));
            JObject jsonObject = JObject.Parse(data);
            foreach (var entity in jsonObject.Last.First.Children())
            {
                var rings = entity["geometry"].First;
                var geographical_name = entity["attributes"]["GEOG_IME"];
                foreach (var ring in rings.Children())
                {
                    foreach (var point in ring.First.Children())
                    {
                        double x = point.First.ToObject<double>();
                        double y = point.Last.ToObject<double>();
                    }
                }
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
