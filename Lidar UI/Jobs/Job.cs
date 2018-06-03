using Lidar_UI.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static Lidar_UI.Tile;

namespace Lidar_UI
{
    public abstract class Job
    {
        public enum JobStatuses
        {
            WAITING,
            RUNNING,
            FINISHED,
            FAILED
        }

        public abstract Stages Start { get;  }
        public abstract Stages Progress { get; }
        public abstract Stages End { get; }

        public JobStatuses Status {
            get {
                if (process == null) return JobStatuses.WAITING;
                else if (process.HasExited) return JobStatuses.RUNNING;
                else if (process.ExitCode == 0) return JobStatuses.FINISHED;
                else return JobStatuses.FAILED;
            }
        }

        Tile tile;

        public FileInfo StartFile => tile.Files[Start];
        public FileInfo ProgressFile => tile.Files[Progress];
        public FileInfo EndFile => tile.Files[End];

        private Process process;

        public string Output;
        public DateTime Started;

        public Job(Tile tile)
        {
            this.tile = tile;
        }

        public Task Run(bool cleanup = false)
        {
            Output = "";
            Started = DateTime.Now;
            return Task.Run(() =>
            {
                string filename = tile.id.GetFilename(Progress);
                FileInfo progressFile = new FileInfo(Path.Combine(StartFile.DirectoryName, tile.id.GetFilename(Progress)));
                bool result = RunProcess(tile);
                if (result) {
                    string newFilename = Path.Combine(StartFile.DirectoryName, tile.id.GetFilename(End));
                    if (progressFile.Exists)
                    {
                        progressFile.MoveTo(newFilename);
                    }
                    if (cleanup){
                        if(StartFile.Exists && !IsFileLocked(StartFile)) StartFile.Delete();
                    }
                }
            });
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private bool RunProcess(Tile t)
        {
            process = GetProcess(t);
            
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, args) => Output += args.Data + "\n";
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        public abstract Process GetProcess(Tile t);

        public static Job NextJob(Tile tile)
        {
            switch (tile.Stage)
            {
                case Stages.Unknown:
                    return new DownloadJob(tile);
                case Stages.Downloaded:
                    return null;
                case Stages.Water:
                    return null;
                case Stages.Colors:
                    return null;
                case Stages.Missing:
                case Stages.Downloading:
                case Stages.AddingWater:
                case Stages.AddingNormals:
                case Stages.AddingColors:
                case Stages.Normals:
                default:
                    return null;
            }
        }

        public abstract string TaskName { get; }

        public string StartedString => Started.ToString("dd.MM.yyyy HH:mm:ss");

        public string TileString => tile.id.ToString();
    }
}
