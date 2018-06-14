using ClipperLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lidar_UI
{
    public class Municipalities
    {
        public Dictionary<int, Municipality> municipalities = new Dictionary<int, Municipality>();
        public Dictionary<TileId, int> map = new Dictionary<TileId, int>();
        const string filename = "municipalities.json";

        public Municipalities()
        {
            if (File.Exists(filename)) Read();
            else
            {
                Load();
                BuildMap(374, 30, 624, 194);
                Write();
            }
        }

        double Intersection(List<IntPoint> polygon, Municipality municipality)
        {
            Clipper c = new Clipper();
            c.AddPolygon(polygon, PolyType.ptSubject);
            c.AddPolygon(municipality.polygon, PolyType.ptClip);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            c.Execute(ClipType.ctIntersection, solution);
            return solution.Sum(s => Clipper.Area(s));
        }

        public Municipality Find(TileId tile)
        {
            var tilePolygon = tile.Polygon;
            return municipalities.Values
                .OrderByDescending(m => Intersection(tilePolygon, m))
                .First();
        }

        public Municipality FindNearest(TileId tile)
        {
            int x = (tile.Left + tile.Right) / 2;
            int y = (tile.Bottom + tile.Top) / 2;
            var mun = municipalities.Values.OrderBy(m =>
                {
                    return Math.Pow(x - m.X, 2) + Math.Pow(y - m.Y, 2);
                }
            ).First();
            return mun;
        }

        public void Read()
        {
            string data = File.ReadAllText(filename);
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new TileIdConverter());
            JsonConvert.PopulateObject(data, this, settings);
        }

        public void Write()
        {
            string data = JsonConvert.SerializeObject(this);
            File.WriteAllText(filename, data);
        }

        public void Load()
        {
            string url = GetUrl(374000, 30000, 624000, 194000, true);
            WebClient client = new WebClient();
            var data = Encoding.UTF8.GetString(client.DownloadData(url));
            JObject jsonObject = JObject.Parse(data);
            foreach (var entity in jsonObject.Last.First.Children())
            {
                var m = new Municipality(entity);
                municipalities[m.Id] = m;
            }
        }

        public void BuildMap(int xmin, int ymin, int xmax, int ymax)
        {
            map = new Dictionary<TileId, int>();
            for (int x = xmin; x <= xmax; x++)
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    TileId id = new TileId(x, y);
                    var m = Find(id);
                    if(Intersection(id.Polygon, m) == 0)
                    {
                        m = FindNearest(id);
                    }
                    map[id] = m.Id;
                }
            }
        }

        private string GetUrl(int xmin, int ymin, int xmax, int ymax, bool geometry = false)
        {
            StringBuilder b = new StringBuilder();
            b.Append("http://gis.arso.gov.si/arcgis/rest/services/AO_UWWTD/MapServer/8/query?");
            b.Append("where=&");
            b.Append("text=&");
            b.Append("objectIds=&");
            b.Append("time=&");
            b.Append("geometry=%7Bxmin%3A+" + xmin + "%2C+xmax%3A+" + xmax + "%2C+ymin%3A+" + ymin + "%2C+ymax%3A+" + ymax + "%7D&");
            b.Append("geometryType=esriGeometryEnvelope&");
            b.Append("inSR=&");
            b.Append("spatialRel=esriSpatialRelIntersects&");
            b.Append("relationParam=&");
            b.Append("outFields=OB_IME%2C+POVRSINA%2C+OB_ID%2C+OB_UIME&");
            b.Append("returnGeometry="+(geometry?"true":"false")+"&");
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
            b.Append("f=json");
            return b.ToString();
        }
    }

    public class TileIdConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRaw(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var intermediateDictionary = new Dictionary<string, int>();
            serializer.Populate(reader, intermediateDictionary);
            Dictionary<TileId, int> finalDictionary = new Dictionary<TileId, int>();
            foreach (var pair in intermediateDictionary)
            {
                var s = pair.Key.Split(' ');
                var key =  new TileId(Convert.ToInt32(s[0]), Convert.ToInt32(s[1]));
                finalDictionary[key] = pair.Value;
            }

            return finalDictionary;
        }

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<TileId, int>);
        }
    }

}
