using ClipperLib;
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
        public int X, Y;
        private static readonly int[] SlovenianMapBounds = { 374, 30, 624, 194 };
        public TileId(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public TileId(string filename)
        {
            string[] parts = filename.Replace(".laz", "").Split('-');
            X = Convert.ToInt32(parts[1]);
            Y = Convert.ToInt32(parts[2]);
        }

        public string GetFilename(Stages stage)
        {
            return ((int)stage)+"-"+X+"-"+Y + ".laz";
        }

        public override string ToString()
        {
            return X + " " + Y;
        }

        public int Left => X * 1000;
        public int Right => (X + 1) * 1000;
        public int Bottom => Y * 1000;
        public int Top => (Y + 1) * 1000;

        public List<IntPoint> Polygon => new List<IntPoint>() {
            new IntPoint(Left, Bottom),
            new IntPoint(Right, Bottom),
            new IntPoint(Right, Top),
            new IntPoint(Left, Top)
        };

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(TileId)) return false;
            TileId t = (TileId)obj;
            return t.X == X && t.Y == Y;
        }
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
        public TileId Id { get; private set; }

        public Dictionary<Stages, FileInfo> Files = new Dictionary<Stages, FileInfo>();

        public int FailedCount = 0;

        public Color TileColor => (new Color[] {
            Colors.DarkGray,
            Colors.Black,
            Colors.Red,
            Color.FromRgb(254, 80, 0),
            Color.FromRgb(254, 155, 0),
            Color.FromRgb(254, 216, 0),
            Color.FromRgb(216, 234, 0),
            Color.FromRgb(165, 200, 0),
            Color.FromRgb(96, 176, 0),
            Color.FromRgb(0, 130, 0),
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
            Id = new TileId(Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]));
            Files[(Stages)Convert.ToInt32(parts[0])] = file;
        }

        public Tile(TileId id, DirectoryInfo path)
        {
            this.Id = id;
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
                var file = new FileInfo(Path.Combine(path.FullName, Id.GetFilename((Stages)i)));
                if (file.Exists) Files[(Stages)i] = file;
            }
        }
    }
}
