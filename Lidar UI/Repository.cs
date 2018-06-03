﻿using System;
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

        public int Width => Right - Left + 1;
        public int Height => Top - Bottom + 1;

        byte[] pixels1d;
        public WriteableBitmap Wbitmap { get; private set; }

        public DirectoryInfo directory { get; private set; }
        public Dispatcher dispatcher;

        public Repository()
        {
            Wbitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Rgb24, null);
            pixels1d = new byte[Height * Width * 3];
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

        public void UpdateTile(Tile tile)
        {
            FillBlock(tile.id.x, tile.id.y, tile.TileColor);
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

        public void Load(DirectoryInfo directory)
        {
            this.directory = directory;
            ResetMap();

            foreach (var file in directory.GetFiles())
            {
                if (file.FullName.EndsWith(".laz"))
                {
                    try
                    {
                        var s = (Stages)Convert.ToInt32(file.Name.Split('-')[0]);

                        TileId id = new TileId(file.Name);
                        if (Tiles.ContainsKey(id)) Tiles[id].AddFile(file);
                        else Tiles[id] = new Tile(file);
                        FillBlock(id.x, id.y, Tiles[id].TileColor);
                    }
                    catch { }
                    
                }
            }
            
            UpdateMap();
        }
    }
}