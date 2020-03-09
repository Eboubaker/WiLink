
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using WPFUI;

namespace Core
{
    class ReceiveOptions
    {
        public IPEndPoint addrrr;
        public string outputPath;

        public ReceiveOptions(IPEndPoint addrrr, string outputPath)
        {
            this.addrrr = addrrr;
            this.outputPath = outputPath;
        }
    }
    class ReceiveOperation
    {
        public static double totalSize;
        public static double[] totalReceived = { 0 };

        public static object safeLock = new object();
        public static Dictionary<int, Item> items;
        public static void Receive(object reciveoptions)
        {
            ReceiveOptions receiveopt = reciveoptions as ReceiveOptions;
            IPEndPoint conn = receiveopt.addrrr;
            string outputPath = receiveopt.outputPath;
            
            Socket sender;
            int itemID = 0;
            Item item = null;
            NetworkStream stream;
            BinaryWriter wrt;
            FileStream file;
            FileInfo fileinfo;
            int read;
            int tid = Thread.CurrentThread.ManagedThreadId;

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
                Console.WriteLine("ReceiverOP{0}: Connecting to Sender", tid);
                if (!Network.SocketConnectWithTimeout(out sender, conn, 10000))
                {
                    Console.WriteLine("ReceiverOP{0}: No Sender Found... Time out 10 seconds Stopping", tid);
                    Utility.Exit("");
                }
                Console.WriteLine("ReceiverOP{0}: Getting Lock(Get Next Key)", tid);
                lock (safeLock)
                {
                    Console.WriteLine("ReceiverOP{0}: Got Lock(Get Next Key)", tid);
                    foreach (int key in items.Keys)
                    {
                        itemID = key;
                        item = items[key];
                        items.Remove(key);
                        break;
                    }
                    stream = new NetworkStream(sender);
                    wrt = new BinaryWriter(stream);
                    Console.WriteLine("ReceiverOP{0}: UnLocking(Get Next Key)", tid);
                }
                if (item == null)
                {
                    Console.WriteLine("ReceiverOP{1}: Sending End of Transmission", itemID, tid);
                    wrt.Write(-1);// end of Operation
                    break;
                }
                else
                {
                    Console.WriteLine("ReceiverOP{1}: Requesting item {0}", itemID, tid);
                    wrt.Write(itemID);// WriteInt32()
                }
                if (item as FileItem != null)
                {
                    
                    FileItem fileitem = item as FileItem;
                    fileitem.ConstructLocalPath(outputPath);
                    if (fileitem.IsDirectory)
                    {
                        Console.WriteLine("ReceiverOP{1}: Creating Directory [{0}]", fileitem.GlobalPath, tid);
                        Directory.CreateDirectory(fileitem.LocalPath);
                    }
                    else
                    {
                        Console.WriteLine("ReceiverOP{1}: Preparing File [{0}]", fileitem.GlobalPath, tid);
                        fileinfo = new FileInfo(fileitem.LocalPath);
                        if (!new DirectoryInfo(fileinfo.DirectoryName).Exists)
                            new DirectoryInfo(fileinfo.DirectoryName).Create();
                        file = File.OpenWrite(fileitem.LocalPath);
                        Console.WriteLine("ReceiverOP{1}: Reading File [{0}]", fileitem.GlobalPath, tid);
                        byte[] buffer = new byte[Constants.BUFFER_SIZE];
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            file.Write(buffer, 0, read);
                            lock (totalReceived)
                            {
                                totalReceived[0] += read;
                            }
                        }
                        file.Close();
                    }
                }
                /*
                Console.WriteLine("ReceiverOP{0}: Locking(Remove Key)", tid);
                lock (safeLock) {
                    Console.WriteLine("ReceiverOP{0}: Got Lock(Remove Key)", tid);
                    Console.WriteLine("ReceiverOP{1}: Destroying item {0}", itemID, tid);
                    items.Remove(itemID);
                    Console.WriteLine("ReceiverOP{0}: UnLocking(Remove Key)", tid);
                }
                */
                Console.WriteLine("ReceiverOP{0}: Closing Connection", tid);
                sender.Shutdown(SocketShutdown.Receive);
                stream.Close();
                sender.Close();
                item = null;
            }
        }
    }
}
