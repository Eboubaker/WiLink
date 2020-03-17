using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Core
{
    [Serializable]
    class MetaInfo
    {
        public TransferItems Items;
        public string SenderName;
        public int[] Ports = new int[2];
        public MetaInfo(TransferItems items)
        {
            Items = items;
            SenderName = Environment.MachineName;
            Ports[0] = 11633;
            Ports[1] = 11933;
        }
        public void SendMetas(IPAddress networkIP)
        {
            Socket receiver;
            if(!Network.SocketAcceptWithTimeout(out receiver, new IPEndPoint(networkIP, Constants.META_PORT), 5000))
            {
                Console.WriteLine("Failed to Send Meta Data - timeout reached (5seconds)");
                Environment.Exit(1);
            }
            BufferedStream stream = new BufferedStream(new NetworkStream(receiver), Constants.BUFFER_SIZE);
            new BinaryFormatter().Serialize(stream, this);
            stream.Flush();
            receiver.Shutdown(SocketShutdown.Both);
            stream.Close();
            receiver.Close();
        }
        public static MetaInfo ReceiveMetas(IPAddress addrr)
        {
            Socket socket;
            if (!Network.SocketConnectWithTimeout(out socket, new IPEndPoint(addrr, Constants.META_PORT), 5000))
            {
                Console.WriteLine("Failed to Get Meta Data - timeout reached (5seconds)");
                Environment.Exit(1);
            }
            NetworkStream stream = new NetworkStream(socket);

            MetaInfo ret = new BinaryFormatter().Deserialize(stream) as MetaInfo;
            socket.Shutdown(SocketShutdown.Both);
            stream.Close();
            socket.Close();
            return ret;
        }
    }
}
