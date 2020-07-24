using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidar_UI.Jobs
{
    class DownloadJob : Job
    {
        public override Stages Start => Stages.Unknown;
        public override Stages Progress => Stages.Downloading;
        public override Stages End => Stages.Downloaded;

        public DownloadJob(Tile tile) : base(tile)
        {

        }

        public override Process GetProcess(Tile t)
        {
            Process p = new Process();
            p.StartInfo.FileName = "DownloadWorker/DownloadWorker.exe";
            p.StartInfo.Arguments = "\""+StartFile.Directory + "\" " + t.Id.X + " " + t.Id.Y;
            p.StartInfo.WorkingDirectory = "./DownloadWorker/";
            return p;
        }

        public override string TaskName => "Download";
    }
}
