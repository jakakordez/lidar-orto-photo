using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WaterWorker
{
    class Program
    {
        static int idx = 0;
        static Pen[] pens = new Pen[] { Pens.Red, Pens.Green, Pens.Blue, Pens.White, Pens.Yellow, Pens.Orange, Pens.Pink };

        static void DrawHoles(ILinearRing ring, Graphics g, Pen pen, int w, int h, double minx, double miny)
        {
            double? xp = null;
            double? yp = null;
            foreach (var coordinate in ring.Coordinates)
            {
                var xd = (coordinate.X - minx) / 8;
                var yd = (coordinate.Y - miny) / 8;
                if (xp.HasValue && yp.HasValue)
                {
                    g.DrawLine(pen, new System.Drawing.Point(Math.Min((int)xd, w - 1), Math.Min((int)yd, h - 1)),
                        new System.Drawing.Point(Math.Min((int)xp, w - 1), Math.Min((int)yp, h - 1)));
                }
                xp = xd;
                yp = yd;
            }
        }

        static void DrawPolygon(DotSpatial.Topology.Polygon p, Graphics g, Pen pen, int w, int h, double minx, double miny)
        {
            if (p.Area < 5000) return;
            DrawHoles(p.Shell, g, pen, w, h, minx, miny);
            foreach (var hole in p.Holes)
            {
                DrawHoles(hole, g, pen, w, h, minx, miny);
            }
        }

        static int Main(string[] args)
        {
            /*var file = DotSpatial.Data.Shapefile.OpenFile(@"C:\Users\jakak\Desktop\topografska karta\T5022V5000_pA.shp");

            Bitmap bmp = new Bitmap((int)file.Extent.Width/8, (int)file.Extent.Height/8);
            var g = Graphics.FromImage(bmp);
            g.Clear(Color.Black);
            
            
            foreach (var feature in file.Features)
            {
                if(feature.BasicGeometry.GetType() == typeof(DotSpatial.Topology.Polygon))
                {
                    DrawPolygon(feature.BasicGeometry as DotSpatial.Topology.Polygon, g, pens[idx], bmp.Width, bmp.Height, file.Extent.MinX, file.Extent.MinY);
                    idx = (idx + 1) % pens.Count();
                }
                else if (feature.BasicGeometry.GetType() == typeof(MultiPolygon))
                {
                    MultiPolygon mp = feature.BasicGeometry as MultiPolygon;
                    foreach (var geo in mp.Geometries)
                    {
                        DrawPolygon(geo as DotSpatial.Topology.Polygon, g, pens[idx], bmp.Width, bmp.Height, file.Extent.MinX, file.Extent.MinY);
                        idx = (idx + 1) % pens.Count();
                    }
                }
                
            }
            bmp.Save(@"C:\Users\jakak\Desktop\output.bmp");*/
            
            string path;
            int x, y;
            try
            {
                path = args[0];
                x = Convert.ToInt32(args[1]);
                y = Convert.ToInt32(args[2]);
            }
            catch
            {
                Console.WriteLine("Usage: WaterWorker PATH X Y");
                Console.WriteLine("Where:");
                Console.WriteLine("  PATH - repository path");
                Console.WriteLine("  X - x coordinate (374 - 624)");
                Console.WriteLine("  y - y coordinate (30 - 194)");
                Console.WriteLine("");
                Console.WriteLine("Received " + args.Length + " arguments:");
                foreach (var item in args)
                {
                    Console.WriteLine(item);
                }
                return 1;
            }
            //try{
                Console.WriteLine("[{0:hh:mm:ss}] Started adding water to tile {1} {2} ", DateTime.Now, x, y);
                Loader l = new Loader(x, y, path);
                l.Start();
            /*}
            catch (WebException e)
            {
                Console.WriteLine("Web exception ");
                if (e.Response is HttpWebRequest) Console.WriteLine("Code: " + ((HttpWebResponse)e.Response).StatusCode);
                Console.WriteLine(e.Response.ResponseUri.AbsoluteUri);
                var response = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(response);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return 1;
            }*/
            return 0;
        }
    }
}
