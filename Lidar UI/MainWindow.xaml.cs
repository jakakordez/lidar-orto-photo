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
using lidar_orto_photo;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lidar_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int[] SlovenianMapBounds = { 374, 30, 624, 194 }; //minx,miny,maxx,maxy in thousand, manualy set based on ARSO website
        Thread worker;

        public MainWindow()
        {
            InitializeComponent();
            txtPath.Text = Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.FullName + "\\resources\\";
            mapView.Load(SlovenianMapBounds);
            Console.SetOut(new OutputWriter(lstOutput));
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (worker == null)
            {
                worker = new Thread(new ParameterizedThreadStart((object parameter) =>
                {
                    var param = (Tuple<bool, string>)parameter;
                    int index = 0;
                    for (var x = SlovenianMapBounds[0]; x <= SlovenianMapBounds[2]; x++)
                    {
                        for (var y = SlovenianMapBounds[1]; y <= SlovenianMapBounds[3]; y++)
                        {
                            var url = Loader.GetArsoUrl(x + "_" + y);
                            if (url == null) mapView.FillBlock(x, y, Colors.Red);
                            else {
                                Console.WriteLine("[{0:hh:mm:ss}] Found URL: {1}", DateTime.Now, url);
                                mapView.FillBlock(x, y, Colors.Yellow);
                                Loader l = new Loader(index, url, param.Item2, param.Item1);
                                l.Start();
                                index++;
                                mapView.FillBlock(x, y, Colors.Green);
                                Console.WriteLine("[{0:hh:mm:ss}] Number of blocs proccesed:  {1}\n", DateTime.Now, index);
                            }
                        }
                    }
                }));
                worker.Start(new Tuple<bool, string>(chkNormals.IsChecked??false, txtPath.Text));
                btnStart.Content = "Stop";
                chkNormals.IsEnabled = false;
                txtPath.IsEnabled = false;
            }
            else
            {
                worker.Abort();
                worker = null;
                chkNormals.IsEnabled = true;
                txtPath.IsEnabled = true;
                btnStart.Content = "Start";
            }
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtPath.Text;
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = dialog.SelectedPath;
            }
        }
    }
}
