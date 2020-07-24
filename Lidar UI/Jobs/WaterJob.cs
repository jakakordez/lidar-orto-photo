using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidar_UI.Jobs
{
    class WaterJob : Job
    {
        public override Stages Start => Stages.Downloaded;
        public override Stages Progress => Stages.AddingWater;
        public override Stages End => Stages.Water;

        public WaterJob(Tile tile) : base(tile)
        {

        }

        public override Process GetProcess(Tile t)
        {
            Process p = new Process();
            p.StartInfo.FileName = "WaterWorker/WaterWorker.exe";
            p.StartInfo.Arguments = "\"" + StartFile.Directory + "\" " + t.Id.X + " " + t.Id.Y;
            p.StartInfo.WorkingDirectory = "./WaterWorker/";
            return p;
        }

        public override string TaskName => "Water";
    }
}
