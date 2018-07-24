using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WaterWorker
{
    class Segment
    {
        public XYZ p1, p2;

        public Segment(XYZ p1, XYZ p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        double width => Math.Abs(p2.x - p1.x);
        double height => Math.Abs(p2.y - p1.y);

        double Left => Math.Min(p1.x, p2.x);

        double Right => Math.Max(p1.x, p2.x);

        double Bottom => Math.Min(p1.y, p2.y);

        XYZ BottomNode => p1.y < p2.y ? p1 : p2;
        XYZ LeftNode => p1.x < p2.x ? p1 : p2;

        public bool Croses(XYZ point)
        {
            if (p1.x < point.x && p2.x < point.x) return false;
            if (p1.y < point.y && p2.y < point.y) return false;
            if (p1.y > point.y && p2.y > point.y) return false;
            double i = ((point.y - Bottom) / height) * width;
            if (BottomNode == LeftNode) i += Left;
            else i = Right - i;
            return i >= point.x;
        }
    }

    public class SegmentTests
    {
        [Fact]
        public void Crosses_1()
        {
            Segment sut = new Segment(new XYZ() { x = 9.6, y = 11.86 }, new XYZ() { x = 6.81, y = 14.51 });
            Assert.False(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
        [Fact]
        public void Crosses_2()
        {
            Segment sut = new Segment(new XYZ() { x = 10.68, y = 12.34 }, new XYZ() { x = 6.3, y = 16.46 });
            Assert.True(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
        [Fact]
        public void Crosses_3()
        {
            Segment sut = new Segment(new XYZ() { x = 6.36, y = 11.42 }, new XYZ() { x = 8.61, y = 15.71 });
            Assert.False(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
        [Fact]
        public void Crosses_4()
        {
            Segment sut = new Segment(new XYZ() { x = 7.34, y = 11.81 }, new XYZ() { x = 15.66, y = 15.26 });
            Assert.True(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }

        [Fact]
        public void Crosses_1r()
        {
            Segment sut = new Segment(new XYZ() { x = 6.81, y = 14.51 }, new XYZ() { x = 9.6, y = 11.86 });
            Assert.False(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
        [Fact]
        public void Crosses_2r()
        {
            Segment sut = new Segment(new XYZ() { x = 6.3, y = 16.46 }, new XYZ() { x = 10.68, y = 12.34 });
            Assert.True(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
        [Fact]
        public void Crosses_3r()
        {
            Segment sut = new Segment(new XYZ() { x = 8.61, y = 15.71 }, new XYZ() { x = 6.36, y = 11.42 });
            Assert.False(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
        [Fact]
        public void Crosses_4r()
        {
            Segment sut = new Segment(new XYZ() { x = 15.66, y = 15.26 }, new XYZ() { x = 7.34, y = 11.81 });
            Assert.True(sut.Croses(new XYZ() { x = 8, y = 14 }));
        }
    }
}
