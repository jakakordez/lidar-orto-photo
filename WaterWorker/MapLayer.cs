using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSPolygon = DotSpatial.Topology.Polygon;

namespace WaterWorker
{
    class MapLayer
    {
        List<Shapefile> shapefiles = new List<Shapefile>();

        public MapLayer(string folder)
        {
            List<string> files = Directory.GetFiles(folder, "*_pA.shp").ToList();
            
            files.AddRange(Directory.GetFiles(folder, "*_pAH.shp").ToList());
            List<int> ids = new List<int>();
            foreach (var file in files)
            {
                var f = Shapefile.OpenFile(file);
                shapefiles.Add(f);
            }
        }

        internal List<Polygon> GetPolygons(int x, int y)
        {
            int xmin = x * 1000;
            int ymin = y * 1000;
            int xmax = (x + 1) * 1000;
            int ymax = (y + 1) * 1000;

            var pp = new DSPolygon(new Coordinate[] {
                new Coordinate(xmin, ymin),
                new Coordinate(xmax, ymin),
                new Coordinate(xmin, ymax),
                new Coordinate(xmax, ymax)});

            var features = shapefiles
                .Where(f => f.Extent.ToEnvelope().Intersects(pp.Envelope))
                .SelectMany(f => f.Features);

            return features
                .Select(f => f.BasicGeometry)
                .SelectMany(f => 
                        (f.GetType() == typeof(DSPolygon)) 
                        ? new DSPolygon[] {f as DSPolygon} 
                        : (f as MultiPolygon).Geometries.Select(g => g as DSPolygon)
                    )
                .Select(p => new Polygon(p))
                .ToList();
        }

        internal List<IPolygon> GetIPolygons(int x, int y)
        {
            int xmin = x * 1000;
            int ymin = y * 1000;
            int xmax = (x + 1) * 1000;
            int ymax = (y + 1) * 1000;
            var coords = new Coordinate[] {
                new Coordinate(xmax, ymax),
                new Coordinate(xmin, ymax),
                new Coordinate(xmax, ymin),
                new Coordinate(xmin, ymin),
                new Coordinate(xmax, ymax)
            };

            var coords2 = new Coordinate[] {
                new Coordinate(xmax+10, ymax),
                new Coordinate(xmin+10, ymax),
                new Coordinate(xmax+10, ymin),
                new Coordinate(xmin+10, ymin),
                new Coordinate(xmax+10, ymax)
            };
            var envelope = new LinearRing(coords);
            var lr = envelope.EnvelopeAsGeometry;

            var features = shapefiles
                .Where(f => envelope.Intersects(f.Extent.ToEnvelope()))
                .SelectMany(f => f.Features);
            return features
                .Select(f => f.BasicGeometry)
                .SelectMany(f =>
                        (f.GetType() == typeof(DSPolygon))
                        ? new DSPolygon[] { f as DSPolygon }
                        : (f as MultiPolygon).Geometries.Select(g => g as DSPolygon)
                    )
                .Where(p => p.Intersects(lr))
                .Select(p => (IPolygon)new GeometryPolygon(p.Intersection(lr)))
                .ToList();
        }
    }
}
