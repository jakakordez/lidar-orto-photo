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
    public partial class MapView : UserControl
    {
        Repository repository;
        public delegate void TileEvent(TileId tileId);
        public event TileEvent TileSelected;
        public event TileEvent TileClicked;
        public event EventHandler Unselected;

        public MapView()
        {
            InitializeComponent();
        }

        public void Load(Repository repository)
        {
            this.repository = repository;
            imgMap.Source = repository.Wbitmap;
            repository.dispatcher = Dispatcher;
        }

        private void imgMap_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(this);

            double horizontalZoom = imgMap.Source.Width / imgMap.ActualWidth;
            double verticalZoom = imgMap.Source.Height / imgMap.ActualHeight;

            var difH = ActualHeight - imgMap.ActualHeight;
            var difW = ActualWidth - imgMap.ActualWidth;

            double zoom = Math.Min(horizontalZoom, verticalZoom);
            var x = (int)Math.Floor((p.X - (difW/2.0)) * zoom) + Repository.Left;
            var y = Repository.Top - (int)Math.Floor((p.Y - (difH/2.0)) * zoom);
            TileSelected?.Invoke(new TileId(x, y));
        }

        private void imgMap_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var p = e.GetPosition(this);

                double horizontalZoom = imgMap.Source.Width / imgMap.ActualWidth;
                double verticalZoom = imgMap.Source.Height / imgMap.ActualHeight;

                var difH = ActualHeight - imgMap.ActualHeight;
                var difW = ActualWidth - imgMap.ActualWidth;

                double zoom = Math.Min(horizontalZoom, verticalZoom);
                var x = (int)Math.Floor((p.X - (difW / 2.0)) * zoom) + Repository.Left;
                var y = Repository.Top - (int)Math.Floor((p.Y - (difH / 2.0)) * zoom);
                TileClicked?.Invoke(new TileId(x, y));
            }
            else Unselected?.Invoke(this, null);
        }
    }
}
