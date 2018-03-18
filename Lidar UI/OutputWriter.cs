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
        TextBox listBox;
        public OutputWriter(TextBox listBox)
        {
            this.listBox = listBox;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string value)
        {
            listBox.Dispatcher.Invoke(() =>
            {
                listBox.Text += value;
                listBox.ScrollToEnd();
            });
        }

        public override void Write(char[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Write(new string(buffer, index, count));
        }
    }
}
