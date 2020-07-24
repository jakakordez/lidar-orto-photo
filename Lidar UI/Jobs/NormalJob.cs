using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lidar_UI.Jobs
{
    class NormalJob : Job
    {
        public override Stages Start => Stages.Colors;
        public override Stages Progress => Stages.AddingNormals;
        public override Stages End => Stages.Normals;

        public NormalJob(Tile tile) : base(tile)
        {

        }

        public override Process GetProcess(Tile t)
        {
            Process p = new Process();
            p.StartInfo.FileName = "NormalWorker/NormalCalculator.exe";
            p.StartInfo.Arguments = "\"" + StartFile.Directory + "\" " + t.Id.X + " " + t.Id.Y;
            p.StartInfo.WorkingDirectory = "./NormalWorker/";
            return p;
        }

        public override string TaskName => "Normal";
    }
}
