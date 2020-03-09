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
    class Receiver
    {
        public static void RecieveFiles(string outputDirectory, IPAddress ip = null)
        {
            if (ip == null && !Network.ConnectToAvailableWiLinkNetwork(out ip))
            {
                Utility.Exit("Failed to Connect To a Network");
            }
            if (outputDirectory == null)
                outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Received");

            //IPHostEntry ipHost = Dns.GetHostEntry("127.0.0.1");
            //IPAddress networkAddress = ipHost.AddressList[0];
            Console.WriteLine("Receiver: Waiting for Sender Signal");
            AwaitSender(ip);
            Console.WriteLine("Receiver: Signal Sent");
            Console.WriteLine("Receiver: Reading Meta info");
            MetaInfo meta = MetaInfo.ReceiveMetas(ip);
            Console.WriteLine("Receiver: Meta info Received");
            Console.WriteLine("Receiver: Starting Operations");
            ReceiveOperation.items = meta.Items.Items;
            ReceiveOperation.totalSize = meta.Items.TotalSize;
            Thread t1 = new Thread(new ParameterizedThreadStart(ReceiveOperation.Receive));
            //Thread t2 = new Thread(new ParameterizedThreadStart(ReceiveOperation.Receive));

            t1.Start(new ReceiveOptions(
                new IPEndPoint(ip, meta.Ports[0]),
                Utility.PathCombine(outputDirectory, meta.SenderName)));
            //t2.Start(new ReceiveOptions(new IPEndPoint(networkAddress, meta.Ports[1]), Utility.PathCombine(outputDirectory, meta.SenderName)));
            
            t1.Join();
            //t2.Join();
        }

        public static bool AwaitSender(IPAddress addrr)
        {
            Socket s;
            bool resp = Network.SocketConnectWithTimeout(out s,
                new IPEndPoint(addrr, Constants.Signal_PORT), 30000);
            if (s!= null && s.Connected)
                s.Close();
            return resp;
        }
    }
}
