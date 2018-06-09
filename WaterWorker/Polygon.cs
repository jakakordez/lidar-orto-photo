using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterWorker
{
    class Polygon
    {
        string geographical_name;
        List<Ring> rings;
        int id;

        public Polygon(Newtonsoft.Json.Linq.JToken entity)
        {
            rings = new List<Ring>();
            var jsonRings = entity["geometry"].First.First;
            geographical_name = entity["attributes"]["GEOG_IME"].ToString();
            id = Convert.ToInt32(entity["attributes"]["OBJECTID"].ToString());
            foreach (var ring in jsonRings.Children())
            {
                rings.Add(new Ring(ring));
            }
        }

        public bool InPolygon(XYZ point)
        {
            if (point.x > Right || point.x < Left || point.y > Top || point.y < Bottom) return false;
            int intersections = rings.Select(s => s.Intersections(point)).Sum();
            return intersections % 2 == 1;
        }

        public double Top => rings.Max(r => r.Top);
        public double Bottom => rings.Min(r => r.Bottom);
        public double Left => rings.Min(r => r.Left);
        public double Right => rings.Max(r => r.Right);
    }
}
