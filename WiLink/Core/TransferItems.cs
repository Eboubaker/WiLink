using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Core
{
    [Serializable]
    class TransferItems
    {
        public long TotalSize = 0;
        public Dictionary<int, Item> Items;
        public void Add(int key, Item item)
        {
            this.TotalSize += item.Size;
            Items.Add(key, item);
        }
        public TransferItems()
        {
            Items = new Dictionary<int, Item>();
        }
    }
}
