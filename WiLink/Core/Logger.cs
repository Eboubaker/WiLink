using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFUI;

namespace Core
{
    public class ControlWriter : TextWriter
    {
        private MainWindow controller;
        public ControlWriter(MainWindow controller)
        {
            this.controller = controller;
        }
        public override void Write(char value)
        {
            controller.Write(value.ToString());
        }
        public override void Write(string value)
        {
            controller.Write(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}
