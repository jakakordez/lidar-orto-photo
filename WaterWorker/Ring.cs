using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Topology;
using Supercluster.KDTree;
using Xunit;

namespace WaterWorker
{
    class XYZ
    {
        public double x, y, z;

        public double DistanceTo(XYZ point)
        {
            return (Math.Sqrt(Math.Pow(Math.Abs(x - point.x), 2) + Math.Pow(Math.Abs(y - point.y), 2)));
        }

        public override string ToString()
        {
            return x + " " + y + " " + z;
        }
        
        public double DistanceSquared(XYZ point)
        {
            return Math.Pow(Math.Abs(x - point.x), 2) + Math.Pow(Math.Abs(y - point.y), 2);
        }

        public static XYZ operator - (XYZ p1, XYZ p2)
        {
            return new XYZ() { x = p2.x - p1.x, y = p2.y - p1.y, z = p2.z - p1.z };
        }

        public static XYZ operator +(XYZ p1, XYZ p2)
        {
            return new XYZ() { x = p2.x + p1.x, y = p2.y + p1.y, z = p2.z + p1.z };
        }

        public static XYZ operator *(double d, XYZ p1)
        {
            p1.x *= d;
            p1.y *= d;
            p1.z *= d;
            return p1;
        }

        public static XYZ operator /(XYZ p1, double d)
        {
            p1.x /= d;
            p1.y /= d;
            p1.z /= d;
            return p1;
        }

        public static double operator *(XYZ p1, XYZ p2)
        {
            return p1.x * p2.x + p1.y * p2.y + p1.z * p2.z;
        }
    }

    class Ring
    {
        public List<XYZ> Points { get; }
        List<Segment> segments;
        
        public Ring(Newtonsoft.Json.Linq.JToken ring)
        {
            Points = new List<XYZ>();
            foreach (var point in ring.Children())
            {
                double x = point.First.ToObject<double>();
                double y = point.Last.ToObject<double>();
                if (x > Right) Right = x;
                if (x < Left) Left = x;
                if (y > Top) Top = y;
                if (y < Bottom) Bottom = y;
                Points.Add(new XYZ() { x = x, y = y });
            }
            segments = new List<Segment>();
            for (int i = 0; i < Points.Count; i++)
            {
                var p1 = Points[i];
                var p2 = Points[(i + 1) % Points.Count];
                segments.Add(new Segment(p1, p2));
            }
        }

        public Ring(ILinearRing ring)
        {
            Points = new List<XYZ>();
            foreach (var point in ring.Coordinates)
            {
                double x = point.X;
                double y = point.Y;
                if (x > Right) Right = x;
                if (x < Left) Left = x;
                if (y > Top) Top = y;
                if (y < Bottom) Bottom = y;
                Points.Add(new XYZ() { x = x, y = y });
            }
            segments = new List<Segment>();
            for (int i = 0; i < Points.Count; i++)
            {
                var p1 = Points[i];
                var p2 = Points[(i + 1) % Points.Count];
                segments.Add(new Segment(p1, p2));
            }
        }

        internal void AssignHeights(KDTree<double, XYZ> tree)
        {
            foreach (var point in Points)
            {
                var pointArray = new double[] { point.x, point.y, point.z };
                var nearset = tree.NearestNeighbors(pointArray, 20);
                var height = nearset.Average(x => x.Item2.z);
                point.z = height;
            }
        }

        internal bool IsOnWater(XYZ point, double offset)
        {
            return Points.TrueForAll(r => r.DistanceTo(point) > offset);
        }

        public double Top = double.MinValue;

        internal int Intersections(XYZ point)
        {
            return segments.Where(s => s.Croses(point)).Count();
        }

        public double Bottom = double.MaxValue;
        public double Right = double.MinValue;
        public double Left = double.MaxValue;

        internal double DistanceFromCoast(XYZ point)
        {
            return segments.Select(s => s.DistanceTo(point)).Min();
            //return Points.Select(p => p.DistanceTo(point)).Min();
        }
    }
}
