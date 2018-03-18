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
        int[] bounds = new int[4];
        Thread worker;

        public MainWindow()
        {
            InitializeComponent();
            txtPath.Text = Directory.GetParent(Directory.GetCurrentDirectory()).Parent?.FullName + "\\resources\\";
            mapView.Load(SlovenianMapBounds);
            Console.SetOut(new OutputWriter(lstOutput));
            SlovenianMapBounds.CopyTo(bounds, 0);
            UpdateValues();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (worker == null)
            {
                worker = new Thread(new ParameterizedThreadStart((object parameter) =>
                {
                    var param = (Tuple<bool, string>)parameter;
                    int index = 0;
                    try
                    {
                        Console.WriteLine("[{0:hh:mm:ss}] Started with bounds: X: {1} - {3}, Y: {2} - {4}", DateTime.Now, bounds[0], bounds[1], bounds[2], bounds[3]);
                        for (var x = bounds[0]; x <= bounds[2]; x++)
                        {
                            for (var y = bounds[1]; y <= bounds[3]; y++)
                            {
                                var url = Loader.GetArsoUrl(x + "_" + y);
                                if (url == null) mapView.FillBlock(x, y, Colors.Red);
                                else
                                {
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
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("Exception");
                        Console.WriteLine(exc.Message);
                        Console.WriteLine(exc.StackTrace);
                    }
                    btnStart_Click(this, null);

                }));
                worker.Start(new Tuple<bool, string>(chkNormals.IsChecked??false, txtPath.Text));
                btnStart.Content = "Stop";
                chkNormals.IsEnabled = false;
                txtPath.IsEnabled = false;
                txtLeft.IsEnabled = false;
                txtBottom.IsEnabled = false;
                txtRight.IsEnabled = false;
                txtTop.IsEnabled = false;
            }
            else
            {
                if(worker.IsAlive) worker.Abort();
                worker = null;
                chkNormals.IsEnabled = true;
                txtPath.IsEnabled = true;
                txtLeft.IsEnabled = true;
                txtBottom.IsEnabled = true;
                txtRight.IsEnabled = true;
                txtTop.IsEnabled = true;
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

        void UpdateValues()
        {
            txtLeft.Text = bounds[0].ToString();
            txtBottom.Text = bounds[1].ToString();
            txtRight.Text = bounds[2].ToString();
            txtTop.Text = bounds[3].ToString();
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            ReadBound(0, txtLeft);
            ReadBound(1, txtBottom);
            ReadBound(2, txtRight);
            ReadBound(3, txtTop);

            mapView.SetArea(bounds);
        }

        void ReadBound(int index, System.Windows.Controls.TextBox txt)
        {
            try
            {
                bounds[index] = Convert.ToInt32(txt.Text);
                txt.Background = Brushes.White;
            }
            catch
            {
                txt.Background = Brushes.Red;
            }
        }
    }
}
