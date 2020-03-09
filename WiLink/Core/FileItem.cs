using System;
using System.IO;
using System.Net.Sockets;
namespace Core
{
    [Serializable]
    class FileItem : Item
    {
        [field: NonSerialized]
        public string LocalPath { get; set; }
        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public string GlobalPath { get; set; }

        public FileItem(string path, string gpath, bool isdir = false)
        {
            LocalPath = path;
            Name = Path.GetFileName(path);
            GlobalPath = gpath;
            IsDirectory = isdir;
            base.Size = !isdir ? new FileInfo(path).Length : 0;
        }

        /**
         * Constructs a local path on the receiver machine
         */
        public string ConstructLocalPath(string outputDir)
        {
            LocalPath = Path.Combine(outputDir, GlobalPath, Name);
            GlobalPath = Path.Combine(GlobalPath, Name);
            return LocalPath;
        }
    }
}
