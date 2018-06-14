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
            //imgMap.ToolTip = new Label() { Content = e.GetPosition(this).X.ToString() };
            
        }
    }
}
