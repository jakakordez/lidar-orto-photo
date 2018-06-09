using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterWorker
{
    class XYZ
    {
        public double x, y, z;
    }

    class Segment
    {
        public XYZ p1, p2;

        public Segment(XYZ p1, XYZ p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        double width => Math.Abs(p2.x - p1.x);
        double height => Math.Abs(p2.y - p1.y);

        double Left => Math.Min(p1.x, p2.x);

        double Bottom => Math.Min(p1.y, p2.y);

        public bool Croses(XYZ point)
        {
            if (p1.x < point.x && p2.x < point.x) return false;
            if (p1.y < point.y && p2.y < point.y) return false;
            if (p1.y > point.y && p2.y > point.y) return false;
            double i = ((point.y - Bottom) / height) * width + Left;
            return i >= point.y;
        }
    }

    class Ring
    {
        List<XYZ> points;
        List<Segment> segments;
        
        public Ring(Newtonsoft.Json.Linq.JToken ring)
        {
            points = new List<XYZ>();
            foreach (var point in ring.Children())
            {
                double x = point.First.ToObject<double>();
                double y = point.Last.ToObject<double>();
                if (x > Right) Right = x;
                if (x < Left) Left = x;
                if (y > Top) Top = y;
                if (y < Bottom) Bottom = y;
                points.Add(new XYZ() { x = x, y = y });
            }
            segments = new List<Segment>();
            for (int i = 0; i < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Count];
                segments.Add(new Segment(p1, p2));
            }
        }

        public double Top = double.MinValue;

        internal int Intersections(XYZ point)
        {
            return segments.Where(s => s.Croses(point)).Count();
        }

        public double Bottom = double.MaxValue;
        public double Right = double.MinValue;
        public double Left = double.MaxValue;
    }
}
