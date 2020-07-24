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
using System.Windows.Shapes;

namespace Lidar_UI
{
    /// <summary>
    /// Interaction logic for MunicipalitiesView.xaml
    /// </summary>
    public partial class MunicipalitiesView : Window
    {
        byte[] pixels1d;
        public WriteableBitmap Wbitmap { get; private set; }

        Repository repository;
        public MunicipalitiesView(Repository repository)
        {
            InitializeComponent();
            this.repository = repository;
            Wbitmap = new WriteableBitmap(repository.Width, repository.Height, 96, 96, PixelFormats.Rgb24, null);
            pixels1d = new byte[repository.Height * repository.Width * 3];

            foreach (var item in repository.Municipalities.map)
            {
                Municipality m = repository.Municipalities.municipalities[item.Value];
                FillBlock(item.Key.X, item.Key.Y, m.Color);
            }

            Int32Rect rect = new Int32Rect(0, 0, repository.Width, repository.Height);
            Wbitmap.WritePixels(rect, pixels1d, 3 * repository.Width, 0);
            mapView.imgMap.Source = Wbitmap;
        }

        private void FillBlock(int x, int y, Color c)
        {
            x = x - Repository.Left;
            y = y - Repository.Bottom;

            y = repository.Height - y - 1;
            pixels1d[y * repository.Width * 3 + x * 3 + 0] = c.R;
            pixels1d[y * repository.Width * 3 + x * 3 + 1] = c.G;
            pixels1d[y * repository.Width * 3 + x * 3 + 2] = c.B;
        }
    }
}
