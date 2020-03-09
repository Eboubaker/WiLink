using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Core
{
    
    public static class ItemType
    {
        public static readonly Type TYPE_MP4 = Instance(0, "MP4");


        private static Type Instance(byte id, string name)
        {
            return new Type(id, name);
        }
        public class Type
        {
            public byte ID;
            public string name;
            internal Type(byte id, string n)
            {
                ID = id;
                name = n;
            }
        }
    }

    [Serializable]
    abstract class Item
    {
        public long Size { set; get; }
        public byte Type { set; get; }
        public double Progress { set; get; }
    }
}
