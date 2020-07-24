using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterWorker
{
    interface IPolygon
    {
        Dictionary<int, XYZ> points { get; }
        Dictionary<int, XYZ> allPoints { get; }
        KDTree<double, XYZ> ringsTree { get; }
        IEnumerable<XYZ> ringPoints { get; }

        bool InPolygon(XYZ point);
        double DistanceFromCoast(XYZ point);
        void AssignHeghts(IEnumerable<XYZ> points);
        List<XYZ> GetGrid(double spacing, double xmin, double ymin, double xmax, double ymax);
        bool IsOnWater(XYZ point, double offset);
        void AssignHeghts();
    }
}
