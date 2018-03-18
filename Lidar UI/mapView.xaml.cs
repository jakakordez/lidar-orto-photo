using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lidar_UI
{
    /// <summary>
    /// Interaction logic for mapView.xaml
    /// </summary>
    public partial class mapView : UserControl
    {
        byte[] pixels1d;
        WriteableBitmap wbitmap;
        int width => mapBounds[2]-mapBounds[0];
        int height => mapBounds[3]-mapBounds[1];
        int[] mapBounds;
        public mapView()
        {
            InitializeComponent();
        }

        public void Load(int[] bounds)
        {
            this.mapBounds = bounds;
            wbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            pixels1d = new byte[height * width * 3];
            imgMap.Source = wbitmap;
        }

        public void SetArea(int[] bounds)
        {
            bounds[0] = Math.Max(bounds[0], mapBounds[0]);
            bounds[1] = Math.Max(bounds[1], mapBounds[1]);
            bounds[2] = Math.Min(bounds[2], mapBounds[2]);
            bounds[3] = Math.Min(bounds[3], mapBounds[3]);

            pixels1d = new byte[height * width * 3];
            imgMap.Source = wbitmap;
            for (int x = bounds[0]; x < bounds[2]; x++)
            {
                for (int y = bounds[1]; y < bounds[3]; y++)
                {
                    FillBlock(x, y, Colors.DarkBlue, false);
                }
            }
            Update();
        }

        public void FillBlock(int x, int y, Color c, bool update = true)
        {
            x = x - mapBounds[0];
            y = y - mapBounds[1];

            y = height - y - 1;
            pixels1d[y * width * 3 + x * 3 + 0] = c.R;
            pixels1d[y * width * 3 + x * 3 + 1] = c.G;
            pixels1d[y * width * 3 + x * 3 + 2] = c.B;

            if (update) Update();
        }

        public void Update()
        {
            Dispatcher.Invoke(() =>
            {
                Int32Rect rect = new Int32Rect(0, 0, width, height);
                wbitmap.WritePixels(rect, pixels1d, 3 * width, 0);
            });
        }
    }
}
