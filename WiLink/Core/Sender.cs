using NativeWifi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    
    class Sender
    {
        public static void SendFiles(IPAddress ip, params string[] paths)
        {
            Console.WriteLine("Hi {0}", "Ahmed");
            if (ip == null)
            {
                if(!Network.StartHostedNetwork(out ip))
                {
                    throw new Exception("Cloudn't Start the application network");
                }
            }
            //IPHostEntry ipHost = Dns.GetHostEntry("127.0.0.1");
            //IPAddress networkIP = ipHost.AddressList[0];

            Console.WriteLine("Sender: Indexing files");
            List<FileItem> filelist = new List<FileItem>();
            foreach(string p in paths)
            {
                filelist.AddRange(Utility.GetFiles(p));
            }
            TransferItems items = new TransferItems();
            for(int i = 0; i < filelist.Count; i++)
            {
                items.Add(i, filelist[i]);
            }
            Console.WriteLine("Sender: Waiting for Receiver Signal");
            if (!AwaitReceiver(ip))
            {
                Console.WriteLine("Sender: No Receivers Found.. Closing, Timeout 30 seconds");
                Environment.Exit(1);
            }
            Console.WriteLine("Sender: Got Signal");
            Console.WriteLine("Sender: Sending Meta Info");
            MetaInfo metainfo = new MetaInfo(items);
            metainfo.SendMetas(ip);
            Console.WriteLine("Sender: Meta Info Sent");
            Console.WriteLine("Sender: Starting Send Operations");
            SendOperation.items = metainfo.Items.Items;
            SendOperation.totalSize = metainfo.Items.TotalSize;
            Thread t1 = new Thread(new ParameterizedThreadStart(SendOperation.Send));
            t1.Start(new IPEndPoint(ip, metainfo.Ports[0]));
            t1.Join();
            Console.WriteLine("Sender: Operations Are Compleat");
        }

        public static bool AwaitReceiver(IPAddress addrr)
        {
            Socket s;
            bool connected = Network.SocketAcceptWithTimeout(out s, new IPEndPoint(addrr, Constants.Signal_PORT), 30000);
            if(s!=null)
                s.Close();
            return connected;
        }
    }
    
}
