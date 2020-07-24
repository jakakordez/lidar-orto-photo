using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using ClipperLib;
using g3;
using Newtonsoft.Json;
using System.Windows.Media;

namespace Lidar_UI
{
    public class Municipality
    {
        public string Name, PrintName;
        public int Id;
        [JsonIgnore]
        private JToken entity;
        private string Url => "http://gis.arso.gov.si/arcgis/rest/services/AO_UWWTD/MapServer/8/" + Id + "?f=pjson";
        [JsonIgnore]
        public List<IntPoint> polygon = new List<IntPoint>();
        //public Polygon2d polygon = new Polygon2d();
        public double X, Y;

        public Color Color => Color.FromRgb((byte)(Id&0xC0), (byte)((Id&0x38)<<2), (byte)((Id&0x7)<<5));

        public Municipality()
        {

        }

        public Municipality(JToken entity)
        {
            this.entity = entity;
            Id = Convert.ToInt32(entity["attributes"]["OB_ID"]);
            Name = entity["attributes"]["OB_IME"].ToString();
            PrintName = entity["attributes"]["OB_UIME"].ToString();
            foreach (var point in entity["geometry"]["rings"].First.Children())
            {
                double x = Convert.ToDouble(point.First);
                double y = Convert.ToDouble(point.Last);
                polygon.Add(new IntPoint((long)x, (long)y));
                //polygon.AppendVertex(new Vector2d(x, y));
            }
            this.X = polygon.Average(p => p.X);
            this.Y = polygon.Average(p => p.Y);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
