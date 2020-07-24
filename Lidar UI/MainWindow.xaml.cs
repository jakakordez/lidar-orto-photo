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
        TileId? selectedTile;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hide();
            repository = new Repository(cmbMunicipalities);
            mapView.Load(repository);

            DirectoryInfo dir;
            try
            {
                dir = new DirectoryInfo(Properties.Settings.Default.repositoryPath);
                if (!dir.Exists) throw new Exception();
            }
            catch
            {
                Properties.Settings.Default.repositoryPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "repository");
                Properties.Settings.Default.Save();
                dir = new DirectoryInfo(Properties.Settings.Default.repositoryPath);
            }
            //var dir = new DirectoryInfo(@"D:\lidar");

            LoadingWindow loader = new LoadingWindow(repository, dir);
            loader.Show();
            await loader.LoadingFinished.Task;

            txtPath.Text = dir.FullName;
            SlovenianMapBounds.CopyTo(bounds, 0);
            UpdateValues();
            jobRunner = new JobRunner(repository);
            lstJobs.ItemsSource = jobRunner.jobs;
            jobRunner.JobFinished += JobRunner_JobFinished;
            jobRunner.JobStarted += JobRunner_JobStarted;
            mapView.TileSelected += MapView_TileSelected;
            mapView.TileClicked += MapView_TileClicked;
            mapView.Unselected += MapView_Unselected;
            Show();
        }

        private void MapView_Unselected(object sender, EventArgs e)
        {
            lstJobs.ItemsSource = jobRunner.jobs;
            selectedTile = null;
        }

        private void MapView_TileClicked(TileId tileId)
        {
            if (jobRunner.jobs != null) lstJobs.ItemsSource = jobRunner.jobs.Where(j => j.Tile.Id.Equals(tileId));
            selectedTile = tileId;
        }

        private void MapView_TileSelected(TileId tileId)
        {
            if (selectedTile == null)
            {
                var stage = "";
                if (repository.Tiles.ContainsKey(tileId)) stage = Enum.GetName(typeof(Stages), repository.Tiles[tileId].Stage);
                var municipality = repository.Municipalities.municipalities?[repository.Municipalities.map[tileId]];
                lblTile.Content = tileId.X + " " + tileId.Y + " " + municipality?.Name + " (" + municipality?.Id + ") " + stage;
            }
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
                repository.UpdateTile(e.Tile);
            });
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnPause.Content = "Pause";
            btnStart.Content = "Stop";
            List<System.Windows.Controls.Control> controls = new List<System.Windows.Controls.Control>()
            {
                chkDownload, chkColor, chkNormals, chkWater, chkCleanup,
                txtPath, txtLeft, txtBottom, txtRight, txtTop, btnStart,
            };
            
            JobRunner.download = chkDownload.IsChecked ?? false;
            JobRunner.color = chkColor.IsChecked ?? false;
            JobRunner.normals = chkNormals.IsChecked ?? false;
            JobRunner.water = chkWater.IsChecked ?? false;
            JobRunner.Cleanup = chkCleanup.IsChecked ?? false;
            controls.ForEach(c => c.IsEnabled = false);
            await jobRunner.RunArea(bounds[0], bounds[1], bounds[2], bounds[3], (Municipality)cmbMunicipalities.SelectedItem);

            controls.ForEach(c => c.IsEnabled = true);
            btnStart.Content = "Start";
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtPath.Text;
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = dialog.SelectedPath;
                repository.Load(new DirectoryInfo(txtPath.Text), null);
                Properties.Settings.Default.repositoryPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            jobRunner.Close();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            btnPause.Content = jobRunner.TogglePause();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (selectedTile != null)
            {
                var municipality = repository.Municipalities.municipalities[repository.Municipalities.map[selectedTile.Value]];
                var mDir = System.IO.Path.Combine(repository.directory.FullName, municipality.Id.ToString());
                if (Directory.Exists(mDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", mDir);
                }
            }
        }
    }
}
