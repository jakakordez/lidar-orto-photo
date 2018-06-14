﻿using ClipperLib;
using g3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Lidar_UI
{
    public struct TileId
    {
        public int x, y;
        private static readonly int[] SlovenianMapBounds = { 374, 30, 624, 194 };
        public TileId(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public TileId(string filename)
        {
            string[] parts = filename.Replace(".laz", "").Split('-');
            x = Convert.ToInt32(parts[1]);
            y = Convert.ToInt32(parts[2]);
        }

        public string GetFilename(Stages stage)
        {
            return ((int)stage)+"-"+x+"-"+y + ".laz";
        }

        public override string ToString()
        {
            return x + " " + y;
        }

        public int Left => x * 1000;
        public int Right => (x + 1) * 1000;
        public int Bottom => y * 1000;
        public int Top => (y + 1) * 1000;

        public List<IntPoint> Polygon => new List<IntPoint>() {
            new IntPoint(Left, Bottom),
            new IntPoint(Right, Bottom),
            new IntPoint(Right, Top),
            new IntPoint(Left, Top)
        };
    }

    public enum Stages
    {
        Unknown,
        Missing,
        Downloading,
        Downloaded,
        AddingWater,
        Water,
        AddingColors,
        Colors,
        AddingNormals,
        Normals
    }

    public class Tile
    {
        public TileId id { get; private set; }

        public Dictionary<Stages, FileInfo> Files = new Dictionary<Stages, FileInfo>();

        public Color TileColor => (new Color[] {
            Colors.DarkGray,
            Colors.DarkBlue,
            Colors.Red,
            Color.FromRgb(120, 68, 62),
            Color.FromRgb(196, 67, 55),
            Color.FromRgb(238, 142, 81),
            Color.FromRgb(237, 190 ,86),
            Color.FromRgb(242, 221, 119),
            Color.FromRgb(210, 226, 90),
            Color.FromRgb(150, 205, 83),
        })[(int)Stage];

        public Stages Stage => Files.Keys.Count > 0 ? Files.Keys.Max() : Stages.Unknown;

        /*public Polygon2d Polygon => new Polygon2d(new Vector2d[] {
            new Vector2d(id.Left, id.Bottom),
            new Vector2d(id.Right, id.Bottom),
            new Vector2d(id.Right, id.Top),
            new Vector2d(id.Left, id.Top)
        });*/

        public Tile(FileInfo file)
        {
            string[] parts = file.Name.Replace(".laz", "").Split('-');
            id = new TileId(Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]));
            Files[(Stages)Convert.ToInt32(parts[0])] = file;
        }

        public Tile(TileId id, DirectoryInfo path)
        {
            this.id = id;
            Files[Stages.Unknown] = new FileInfo(Path.Combine(path.FullName, id.GetFilename(Stages.Unknown)));
            Files[Stages.Unknown].Create();
        }

        public void AddFile(FileInfo newFile)
        {
            string[] parts = newFile.Name.Replace(".laz", "").Split('-');
            Files[(Stages)Convert.ToInt32(parts[0])] = newFile;
        }

        public void Rescan(DirectoryInfo path)
        {
            Files = new Dictionary<Stages, FileInfo>();
            for (int i = 0; i < Enum.GetNames(typeof(Stages)).Length; i++)
            {
                var file = new FileInfo(Path.Combine(path.FullName, id.GetFilename((Stages)i)));
                if (file.Exists) Files[(Stages)i] = file;
            }
        }
    }
}
