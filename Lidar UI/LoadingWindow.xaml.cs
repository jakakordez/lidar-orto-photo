using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        Repository repository;
        DirectoryInfo dir;
        public TaskCompletionSource<bool> LoadingFinished = new TaskCompletionSource<bool>();
        public LoadingWindow(Repository repository, DirectoryInfo dir)
        {
            InitializeComponent();
            this.repository = repository;
            this.dir = dir;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                repository.Load(dir, (percent) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = percent;
                    });
                });
            });
            Close();
            LoadingFinished.SetResult(true);
        }
    }
}
