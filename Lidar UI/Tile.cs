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
                var file = new FileInfo(Path.Combine(path.FullName, id.GetFilename(Stages.Unknown)));
                if (file.Exists) Files[(Stages)i] = file;
            }
        }
    }
}
