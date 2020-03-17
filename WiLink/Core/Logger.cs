using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFUI;

namespace Core
{
    public class Logger : TextWriter
    {
        public static Logger Instance;
        private BufferedStream logfile;
        private int offset = 0;
        public Logger()
        {
            logfile = new BufferedStream(File.OpenWrite("Log" + DateTime.Now.Second + ".log"), 1024);
            Instance = this;
        }
        public override void Write(char value)
        {
            logfile.WriteByte((byte)value);
            offset++;
            logfile.Flush();
        }
        public override void Write(string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            logfile.Write(bytes, 0, bytes.Length);
            logfile.Flush();
        }
        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }

        public void Flush()
        {
            logfile.Flush();
        }

        public void close()
        {
            logfile.Close();
        }
    }
}
