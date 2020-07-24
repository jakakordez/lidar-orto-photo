using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Topology;
using Supercluster.KDTree;

namespace WaterWorker
{
    class GeometryPolygon:IPolygon
    {
        IGeometry geometry;
        public GeometryPolygon(IGeometry geometry)
        {
            this.geometry = geometry;
        }

        public Dictionary<int, XYZ> points { get; set; } = new Dictionary<int, XYZ>();
        public Dictionary<int, XYZ> allPoints { get; set; } = new Dictionary<int, XYZ>();

        public KDTree<double, XYZ> ringsTree { get; set; }

        public IEnumerable<XYZ> ringPoints => geometry.Coordinates.Select(p => new XYZ() { x = p.X, y = p.Y, z = p.Z });

        public void AssignHeghts()
        {
            AssignHeghts(allPoints.Values);
        }

        public void AssignHeghts(IEnumerable<XYZ> allPoints)
        {
            var treeData = allPoints.Select(p => new double[] { p.x, p.y, p.z }).ToArray();
            KDTree<double, XYZ> tree = new KDTree<double, XYZ>(3, treeData, allPoints.ToArray(), Loader.L2Norm);

            foreach (var point in geometry.Coordinates)
            {
                var pointArray = new double[] { point.X, point.Y, point.Z };
                var nearset = tree.NearestNeighbors(pointArray, 20);
                var height = nearset.Average(x => x.Item2.z);
                point.Z = height;
            }

            treeData = geometry.Coordinates.Select(p => new double[] { p.X, p.Y }).ToArray();
            ringsTree = new KDTree<double, XYZ>(2, treeData, ringPoints.ToArray(), Loader.L2Norm);
        }

        public double DistanceFromCoast(XYZ point)
        {
            return geometry.Coordinates.Min(c => c.Distance(new Coordinate(point.x, point.y)));
        }

        public bool InPolygon(XYZ point)
        {
            return geometry.Contains(new Point(point.x, point.y));
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
            for (double x = Math.Max(geometry.Envelope.Left(), xmin); x <= Math.Min(geometry.Envelope.Right(), xmax); x += spacing)
            {
                for (double y = Math.Max(geometry.Envelope.Bottom(), ymin); y <= Math.Min(geometry.Envelope.Top(), ymax); y += spacing)
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
                            point.x += ((r.NextDouble() * spacing) - (spacing / 2)) * 0.5;
                            point.y += ((r.NextDouble() * spacing) - (spacing / 2)) * 0.5;

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

        public bool IsOnWater(XYZ point, double offset)
        {
            return DistanceFromCoast(point) > offset;
        }
    }
}
