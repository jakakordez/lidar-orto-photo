﻿using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterWorker
{
    class Polygon:IPolygon
    {
        string geographical_name;
        public List<Ring> rings;
        int id;
        public Dictionary<int, XYZ> points { get; set; } = new Dictionary<int, XYZ>();
        public Dictionary<int, XYZ> allPoints { get; set; } = new Dictionary<int, XYZ>();


        public override string ToString()
        {
            return geographical_name + " " + id;
        }

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

        public Polygon(DotSpatial.Topology.Polygon polygon)
        {
            rings = new List<Ring>();
            rings.Add(new Ring(polygon.Shell));
            foreach (var hole in polygon.Holes)
            {
                rings.Add(new Ring(hole));
            }
        }

        public bool InPolygon(XYZ point)
        {
            if (point.x > Right || point.x < Left || point.y > Top || point.y < Bottom) return false;
            int intersections = rings.Select(s => s.Intersections(point)).Sum();
            return intersections % 2 == 1;
        }

        public KDTree<double, XYZ> ringsTree { get; set; }

        public IEnumerable<XYZ> ringPoints => rings.SelectMany(r => r.Points);

        public double Top => rings.Max(r => r.Top);
        public double Bottom => rings.Min(r => r.Bottom);
        public double Left => rings.Min(r => r.Left);
        public double Right => rings.Max(r => r.Right);

        public void AssignHeghts()
        {
            AssignHeghts(allPoints.Values);
        }

        public void AssignHeghts(IEnumerable<XYZ>  allPoints)
        {
            var treeData = allPoints.Select(p => new double[] { p.x, p.y, p.z }).ToArray();
            KDTree<double, XYZ> tree = new KDTree<double, XYZ>(3, treeData, allPoints.ToArray(), Loader.L2Norm);

            foreach (var ring in rings)
            {
                ring.AssignHeights(tree);
            }

            treeData = rings.SelectMany(r => r.Points).Select(p => new double[] { p.x, p.y }).ToArray();
            ringsTree = new KDTree<double, XYZ>(2, treeData, rings.SelectMany(r => r.Points).ToArray(), Loader.L2Norm);
        }

        public double DistanceFromCoast(XYZ point)
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
            //if (points.Count == 0) return new List<XYZ>();
            var treeData = points.Values.Select(p => new double[] { p.x, p.y }).ToArray();
            KDTree<double, XYZ> tree = null;
            if (points.Count > 0)
                tree = new KDTree<double, XYZ>(2, treeData, points.Values.ToArray(), Loader.L2Norm);

            //double elevation = points.Select(p => p.Value.z).Average();

            /*double elevation;
            if (points.Count > 0) elevation = points.Select(p => p.Value.z).Average();
            else elevation = allPoints.Select(p => p.Value.z).Min();*/

            Random r = new Random();
            List<XYZ> grid = new List<XYZ>();
            for (double x = Math.Max(Left, xmin); x <= Math.Min(Right, xmax); x += spacing)
            {
                for (double y = Math.Max(Bottom, ymin); y <= Math.Min(Top, ymax); y += spacing)
                {
                    XYZ point = new XYZ() { x = x, y = y };
                    if (InPolygon(point))
                    {
                        bool hasToBeAdded = false;
                        if (points.Count > 0)
                        {
                            var nearestPoints = tree.NearestNeighbors(new double[] { x, y }, 1);
                            var nearest = nearestPoints[0].Item2;
                            hasToBeAdded = point.DistanceTo(nearest) > spacing;
                        }
                        else hasToBeAdded = true;
                            
                        if (hasToBeAdded)
                        {
                            point.x += ((r.NextDouble() * spacing) - (spacing / 2))*0.5;
                            point.y += ((r.NextDouble() * spacing) - (spacing / 2))*0.5;

                            var nearestRing = ringsTree.NearestNeighbors(new double[] { x, y }, 3);
                            point.z = nearestRing.Select(p => p.Item2.z).Average();

                            //point.z = nearestPoints.Select(p => p.Item2.z).Average();
                            grid.Add(point);
                        }
                    }
                }
            }
            return grid;
        }
    }

}
