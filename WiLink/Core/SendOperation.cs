using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WPFUI;

namespace Core
{
    class SendOperation
    {
        public static double[] totalReceived = { 0 };
        public static double totalSize;
        public static Dictionary<int, Item> items;
        public static void Send(object addrro)
        {
            IPEndPoint addrr = (IPEndPoint)addrro;
            int itemID;
            Item item;
            Socket client;
            NetworkStream stream;
            FileStream file = null;
            FileItem fileitem = null;
            int read;
            int tid = Thread.CurrentThread.ManagedThreadId;

            Socket server = new Socket(addrr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(addrr);
            server.Listen(1);

            new Thread(new ThreadStart(() => {
                while (true)
                {
                    lock (totalReceived)
                    {
                        double v = totalReceived[0] / totalSize;
                        MainWindow.Progress.Dispatcher.Invoke((Action)(() =>
                        {
                            MainWindow.Progress.Value = v * 100;
                        }));
                        if (v == 1)
                        {
                            break;
                        }
                    }
                    Thread.Sleep(200);
                }
            })).Start();

            while (true)
            {
                Console.WriteLine("SenderOP{0}: Getting Lock(index Key)");
                Console.WriteLine("SenderOP{0}: Getting Lock(index Key)", tid);
                lock (items)
                {
                    Console.WriteLine("ReceiverOP{0}: Got Lock(index Key)", tid);
                    Console.WriteLine("SenderOP{0}: Waiting for a File request", tid);
                    if (!Network.SocketAcceptWithTimeout(out client, server, 10000))
                    {
                        Console.WriteLine("SenderOP{0}: No Receiver Found... Time out 10 seconds, Stopping", tid);
                        Environment.Exit(1);
                    }
                    stream = new NetworkStream(client);
                    itemID = new BinaryReader(stream).ReadInt32();
                    if (itemID < 0)
                    {
                        Console.WriteLine("SenderOP{1}: Got End of Transmission", itemID, tid);
                        break;
                    }
                    Console.WriteLine("SenderOP{1}: Receiver Requested item {0}", itemID, tid);
                    item = items[itemID];
                    Console.WriteLine("SenderOP{0}: Unlocking(index Key)", tid);
                }
                
                if (item as FileItem != null)
                {
                    try
                    {
                        fileitem = (FileItem)item;
                        Console.WriteLine("SenderOP{1}: Sending {0}", fileitem.GlobalPath, tid);
                        file = File.OpenRead(fileitem.LocalPath);

                        byte[] buffer = new byte[Constants.BUFFER_SIZE];
                        while ((read = file.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, read);
                            lock (totalReceived)
                            {
                                totalReceived[0] += read;
                            }
                        }
                        file.Close();
                    }catch(UnauthorizedAccessException e)
                    {
                        Console.WriteLine("SenderOP{1}: insufficient Authorization to Read from {0} Skipping file...", fileitem.GlobalPath, tid);
                    }
                }
                Console.WriteLine("SenderOP{0}: Closing Socket", tid);
                client.Shutdown(SocketShutdown.Both);
                stream.Close();
                client.Close();
            }
            server.Close();
        }
    }
}
