using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidar_UI.Jobs
{
    class ColorJob : Job
    {
        public override Stages Start => Stages.Water;
        public override Stages Progress => Stages.AddingColors;
        public override Stages End => Stages.Colors;

        public ColorJob(Tile tile) : base(tile)
        {

        }

        public override Process GetProcess(Tile t)
        {
            Process p = new Process();
            p.StartInfo.FileName = "ColorWorker/ColorWorker.exe";
            p.StartInfo.Arguments = "\"" + StartFile.Directory + "\" " + t.Id.X + " " + t.Id.Y;
            p.StartInfo.WorkingDirectory = "./ColorWorker/";
            return p;
        }

        public override string TaskName => "Color";
    }
}
