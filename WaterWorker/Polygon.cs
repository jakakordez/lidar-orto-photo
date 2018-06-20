using Supercluster.KDTree;
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
        public Dictionary<int, XYZ> points = new Dictionary<int, XYZ>();

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

        internal double DistanceFromCoast(XYZ point)
        {
            return rings.Select(r => r.DistanceFromCoast(point)).Min();
        }

        public bool IsOnWater(XYZ point, double offset)
        {
            return rings.TrueForAll(r => r.IsOnWater(point, offset));
        }

        private bool NotAlreadyCovered(XYZ point, double spacing)
        {
            return points.Values.All(p => p.DistanceTo(point) > spacing);
        }

        public List<XYZ> GetGrid(double spacing, double xmin, double ymin, double xmax, double ymax)
        {
            Func<double[], double[], double> L2Norm = (x, y) =>
            {
                double dist = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    dist += (x[i] - y[i]) * (x[i] - y[i]);
                }

                return dist;
            };
            if (points.Count == 0) return new List<XYZ>();
            var treeData = points.Values.Select(p => new double[] { p.x, p.y }).ToArray();
            KDTree<double, XYZ> tree = new KDTree<double, XYZ>(2, treeData, points.Values.ToArray(), L2Norm);

            //double elevation = points.Select(p => p.Value.z).Average();

            Random r = new Random();
            List<XYZ> grid = new List<XYZ>();
            for (double x = Math.Max(Left, xmin); x <= Math.Min(Right, xmax); x+=spacing)
            {
                for (double y = Math.Max(Bottom, ymin); y <= Math.Min(Top, ymax); y += spacing)
                {
                    XYZ point = new XYZ() { x = x, y = y/*, z = elevation */};
                    if (InPolygon(point))
                    {
                        var nearestPoints = tree.NearestNeighbors(new double[] { x, y }, 5);
                        var nearest = nearestPoints[0].Item2;
                        if (point.DistanceTo(nearest) > spacing)
                        {
                            point.x += ((r.NextDouble() * spacing) - (spacing / 2))*0.5;
                            point.y += ((r.NextDouble() * spacing) - (spacing / 2))*0.5;
                            point.z = nearestPoints.Select(p => p.Item2.z).Average();
                            grid.Add(point);
                        }
                    }
                }
            }
            return grid;
        }
    }
}
