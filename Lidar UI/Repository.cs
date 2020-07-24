using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Lidar_UI
{
    public class Repository
    {
        public static readonly int Left = 374;
        public static readonly int Bottom = 30;
        public static readonly int Right = 624;
        public static readonly int Top = 194;
        public static readonly TileId BottomLeft = new TileId(Left, Bottom);
        public static readonly TileId TopRight = new TileId(Right, Top);

        public Dictionary<TileId, Tile> Tiles = new Dictionary<TileId, Tile>();

        public Municipalities Municipalities = new Municipalities();

        public int Width => Right - Left + 1;
        public int Height => Top - Bottom + 1;

        byte[] pixels1d;
        public WriteableBitmap Wbitmap { get; private set; }

        public DirectoryInfo directory { get; private set; }
        public Dispatcher dispatcher;

        public FileStream logFile;
        System.Windows.Controls.ComboBox cmbMunicipalities;

        public Repository(System.Windows.Controls.ComboBox cmbMunicipalities)
        {
            Wbitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Rgb24, null);
            pixels1d = new byte[Height * Width * 3];
            this.cmbMunicipalities = cmbMunicipalities;
            cmbMunicipalities.ItemsSource = Municipalities.municipalities.Select(m => m.Value).OrderBy(m => m.Name);
        }

        internal Tile GenerateTile(TileId id)
        {
            var dir = DirectoryForTile(id);
            if (!dir.Exists) Directory.CreateDirectory(dir.FullName);
            return new Tile(id, dir);
        }

        public void UpdateMap()
        {
            if(dispatcher != null)
            {
                dispatcher.Invoke(() =>
                {
                    Int32Rect rect = new Int32Rect(0, 0, Width, Height);
                    Wbitmap.WritePixels(rect, pixels1d, 3 * Width, 0);
                });
            }
        }

        public DirectoryInfo DirectoryForTile(TileId id)
        {
            string path = Path.Combine(directory.FullName,
                Municipalities.map[id].ToString());
            return new DirectoryInfo(path);
        }

        public void UpdateTile(Tile tile)
        {
            FillBlock(tile.Id.X, tile.Id.Y, tile.TileColor);
        }

        private void FillBlock(int x, int y, Color c, bool update = true)
        {
            x = x - Left;
            y = y - Bottom;

            y = Height - y - 1;
            pixels1d[y * Width * 3 + x * 3 + 0] = c.R;
            pixels1d[y * Width * 3 + x * 3 + 1] = c.G;
            pixels1d[y * Width * 3 + x * 3 + 2] = c.B;

            if (update) UpdateMap();
        }

        private void ResetMap()
        {
            Tiles = new Dictionary<TileId, Tile>();
            for (int x = Left; x <= Right; x++)
            {
                for (int y = Bottom; y <= Top; y++)
                {
                    FillBlock(x, y, Colors.DarkGray, false);
                }
            }
            UpdateMap();
        }

        public void Load(DirectoryInfo directory, Action<double> d)
        {
            logFile?.Flush();
            logFile?.Close();
            this.directory = directory;
            string logFileName = directory.FullName + "/pipeline.log";
            if (!File.Exists(logFileName)) File.Create(logFileName);
            logFile = new FileStream(logFileName, FileMode.Append);
            logFile.Write(new byte[] { 0x31, 0x32, 0x33 }, 0, 3);
            logFile.Flush();

            ResetMap();
            var municipalityDirs = directory.GetDirectories();
            int i = 0;
            foreach (var municipalityDir in municipalityDirs)
            {
                d?.Invoke((double)(++i) / municipalityDirs.Length);
                foreach (var file in municipalityDir.GetFiles())
                {
                    if (file.FullName.EndsWith(".laz"))
                    {
                        try
                        {
                            var s = (Stages)Convert.ToInt32(file.Name.Split('-')[0]);

                            TileId id = new TileId(file.Name);
                            if (Tiles.ContainsKey(id)) Tiles[id].AddFile(file);
                            else Tiles[id] = new Tile(file);
                            FillBlock(id.X, id.Y, Tiles[id].TileColor);
                        }
                        catch { }
                    }
                }
            }
            
            UpdateMap();
        }
    }
}
