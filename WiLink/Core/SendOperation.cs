using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using WPFUI;
using WPFUI.Core;

namespace Core
{
    class SendOperation
    {
        public static Dictionary<int, Item> items;
        static Item currentItem;
        public static void Send(object addrro)
        {
            IPEndPoint addrr = (IPEndPoint)addrro;
            int itemID;
            Socket client;
            NetworkStream stream;
            FileStream file = null;
            FileItem fileitem = null;
            int read;
            Socket server = new Socket(addrr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(addrr);
            server.Listen(1);
            for(int i = 0; i < items.Count; i++)
            {
                MainWindow.ItemsProgress.Add(0);
                MainWindow.ItemsProgressHistory.Add(0);
            }
            new Thread(() => 
                {
                    while (true) 
                    {
                        try 
                        {
                            MainWindow.instance.ThrowTracker();
                        } 
                        catch {}
                        Thread.Sleep(2000);
                    }
                }
            ).Start();

            TestUtil.AddTimer("All Timer");// 0
            TestUtil.AddTimer("Waiting For a Connection");// 1
            TestUtil.AddTimer("Waiting For File Number Request");// 2
            TestUtil.AddTimer("Reading The File Bytes");// 3
            TestUtil.AddTimer("Writing The File Bytes");// 4
            TestUtil.AddTimer("Adding The File Progress");// 5

            TestUtil.StartTimerTicking(0);
            MainWindow.instance.SetStatus("Sending Files");
            while (true)
            {
                if (!SharedAttributes.ServePending)
                {
                    break;
                }
                TestUtil.StartTimerTicking(1);
                if (!Network.SocketAcceptWithTimeout(out client, server, 10000))
                {
                    MessageBox.Show("Receiver Disconnected");
                    break;
                }
                TestUtil.StopTimerTicking(1);

                TestUtil.StartTimerTicking(2);
                stream = new NetworkStream(client);
                itemID = new BinaryReader(stream).ReadInt32();
                TestUtil.StopTimerTicking(2);
                if (itemID < 0)
                {
                    Console.WriteLine("Got End of Transmission");
                    break;
                }
                Console.WriteLine("Receiver Requested item {0}", itemID);
                currentItem = items[itemID];
                if (currentItem as FileItem != null)
                {
                    try
                    {
                        fileitem = (FileItem)currentItem;
                        Console.WriteLine("Sending {0}", fileitem.GlobalPath);

                        file = File.OpenRead(fileitem.LocalPath);
                        byte[] readBuffer = new byte[Constants.BUFFER_SIZE];
                        while (true)
                        {
                            TestUtil.StartTimerTicking(3);
                            read = file.Read(readBuffer, 0, readBuffer.Length);
                            TestUtil.StopTimerTicking(3);
                            if (!(read > 0))
                            {
                                break;
                            }
                            TestUtil.StartTimerTicking(4);
                            stream.Write(readBuffer, 0, read);
                            TestUtil.StopTimerTicking(4);

                            TestUtil.StartTimerTicking(5);
                            currentItem.Progress += read;
                            Monitor.Enter(MainWindow.ItemsProgress);
                            MainWindow.ProgressSize += read;
                            MainWindow.ItemsProgress[currentItem.Id] = (float)(currentItem.Progress / currentItem.Size);
                            Monitor.Exit(MainWindow.ItemsProgress);
                            TestUtil.StopTimerTicking(5);
                        }
                        file.Close();
                    }
                    catch(UnauthorizedAccessException e)
                    {
                        Console.WriteLine("insufficient Authorization to Read from {0} Skipping file...", fileitem.GlobalPath);
                    }
                }
                Console.WriteLine("Closing Socket");
                client.Shutdown(SocketShutdown.Both);
                stream.Close();
                client.Close();
            }
            TestUtil.StopTimerTicking(0);
            server.Close();
            TestUtil.ShowAllTimers();
        }
    }
}
