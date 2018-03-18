using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Lidar_UI
{
    class OutputWriter : TextWriter
    {
        ListBox listBox;
        public OutputWriter(ListBox listBox)
        {
            this.listBox = listBox;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string value)
        {
            base.Write(value);
            listBox.Dispatcher.Invoke(() =>
            {
                listBox.Items.Add(value);
            });
        }
    }
}
