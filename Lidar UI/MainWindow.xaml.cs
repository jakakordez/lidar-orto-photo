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
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace Lidar_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int[] SlovenianMapBounds = { 374, 30, 624, 194 }; //minx,miny,maxx,maxy in thousand, manualy set based on ARSO website
        int[] bounds = new int[4];
        Repository repository;
        JobRunner jobRunner;

        public MainWindow()
        {
            InitializeComponent();
            
            repository = new Repository();
            mapView.Load(repository);
            var dir = new DirectoryInfo(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "repository"));
            repository.Load(dir);
            txtPath.Text = dir.FullName;
            SlovenianMapBounds.CopyTo(bounds, 0);
            UpdateValues();
            jobRunner = new JobRunner(repository);
            lstJobs.ItemsSource = jobRunner.jobs;
            jobRunner.JobFinished += JobRunner_JobFinished;
            jobRunner.JobStarted += JobRunner_JobStarted;
        }

        private void JobRunner_JobStarted(object sender, Job e)
        {
            Dispatcher.Invoke(() => {
                jobRunner.jobs.Add(e);
                lstJobs.ItemsSource = jobRunner.jobs;
                ICollectionView view = CollectionViewSource.GetDefaultView(jobRunner.jobs);
                view.Refresh();
            });
        }

        private void JobRunner_JobFinished(object sender, Job e)
        {
            Dispatcher.Invoke(() => {
                //jobRunner.jobs.Remove(e);
                ICollectionView view = CollectionViewSource.GetDefaultView(jobRunner.jobs);
                view.Refresh();
                lstJobs.ItemsSource = jobRunner.jobs;
                repository.UpdateTile(e.tile);
            });
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.Content = "Stop";
            chkNormals.IsEnabled = false;
            txtPath.IsEnabled = false;
            txtLeft.IsEnabled = false;
            txtBottom.IsEnabled = false;
            txtRight.IsEnabled = false;
            txtTop.IsEnabled = false;
            btnStart.IsEnabled = false;

            JobRunner.download = chkDownload.IsChecked ?? false;
            JobRunner.color = chkColor.IsChecked ?? false;
            JobRunner.normals = chkNormals.IsChecked ?? false;
            JobRunner.water = chkWater.IsChecked ?? false;
            await jobRunner.RunArea(bounds[0], bounds[1], bounds[2], bounds[3]);

            chkNormals.IsEnabled = true;
            txtPath.IsEnabled = true;
            txtLeft.IsEnabled = true;
            txtBottom.IsEnabled = true;
            txtRight.IsEnabled = true;
            txtTop.IsEnabled = true;
            btnStart.IsEnabled = true;
            btnStart.Content = "Start";
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtPath.Text;
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = dialog.SelectedPath;
                repository.Load(new DirectoryInfo(txtPath.Text));
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

            //mapView.SetArea(bounds);
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

        private void lstJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lstJobs.Items.Count > 0
                && lstJobs.SelectedItem != null)
                lstOutput.Text = ((Job)lstJobs.SelectedItem).Output;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MunicipalitiesView v = new MunicipalitiesView(repository);
            v.Show();
        }
    }
}
