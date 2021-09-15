
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using WPFUI;
using WPFUI.Core;

namespace Core
{
    class ReceiveOperation
    {
        public static Item currentItem;
        public static Queue<Item> items;

        public static void Receive(IPEndPoint conn, string outputPath)
        {   
            Socket sender;
            NetworkStream stream;
            BinaryWriter wrt;
            FileStream file;
            FileInfo fileinfo;
            int read;
            int tid = Thread.CurrentThread.ManagedThreadId;

            for (int i = 0; i < items.Count; i++)
            {
                MainWindow.ItemsProgress.Add(0);
                MainWindow.ItemsProgressHistory.Add(0);
            }
            new Thread(() =>
            {
                while (Application.Current != null)
                {
                    try
                    {
                        MainWindow.instance.ThrowTracker();
                    }
                    catch { }
                    Thread.Sleep(2000);
                }
            }
            ).Start();
            
            TestUtil.AddTimer("All Timer");// 0
            TestUtil.AddTimer("Connecting");// 1
            TestUtil.AddTimer("Reading The File Bytes");// 2
            TestUtil.AddTimer("Writing The File Bytes");// 3
            TestUtil.AddTimer("Adding The File Progress");// 4

            TestUtil.StartTimerTicking(0);
            MainWindow.instance.SetStatus("Sending Files");
            while (true)
            {
                if(!SharedAttributes.ReceivePending)
                {
                    break;
                }
                TestUtil.StartTimerTicking(1);
                if (!Network.SocketConnectWithTimeout(out sender, conn, 10000))
                {
                    MessageBox.Show("Sender Disconnected");
                    break;
                }
                TestUtil.StopTimerTicking(1);

                stream = new NetworkStream(sender);
                wrt = new BinaryWriter(stream);
                if (items.Count == 0)
                {
                    wrt.Write(-1);// end of Operation
                    break;
                }
                else
                {
                    Console.WriteLine("Getting Key");
                    currentItem = items.Dequeue();
                    wrt.Write(currentItem.Id);// WriteInt32()
                    Console.WriteLine("Requested item {0}", currentItem.Id);
                }
                if (currentItem as FileItem != null)
                {
                    FileItem fileitem = currentItem as FileItem;
                    fileitem.ConstructLocalPath(outputPath);
                    if (fileitem.IsDirectory)
                    {
                        Console.WriteLine("Creating Directory [{0}]", fileitem.GlobalPath);
                        Directory.CreateDirectory(fileitem.LocalPath);
                    }
                    else
                    {
                        Console.WriteLine("Preparing File [{0}]", fileitem.GlobalPath);
                        fileinfo = new FileInfo(fileitem.LocalPath);
                        if (!new DirectoryInfo(fileinfo.DirectoryName).Exists)
                            new DirectoryInfo(fileinfo.DirectoryName).Create();
                        file = File.OpenWrite(fileitem.LocalPath);
                        Console.WriteLine("Reading File [{0}]", fileitem.GlobalPath);
                        byte[] buffer = new byte[Constants.BUFFER_SIZE];
                        while (true)
                        {
                            TestUtil.StartTimerTicking(2);
                            read = stream.Read(buffer, 0, buffer.Length);
                            TestUtil.StopTimerTicking(2);
                            if (!(read>0))
                            {
                                break;
                            }
                            TestUtil.StartTimerTicking(3);
                            file.Write(buffer, 0, read);
                            TestUtil.StopTimerTicking(3);

                            TestUtil.StartTimerTicking(4);
                            currentItem.Progress += read;
                            Monitor.Enter(MainWindow.ItemsProgress);
                            MainWindow.ProgressSize += read;
                            MainWindow.ItemsProgress[currentItem.Id] = (float)(currentItem.Progress / currentItem.Size);
                            Monitor.Exit(MainWindow.ItemsProgress);
                            TestUtil.StartTimerTicking(4);
                        }
                        file.Close();
                    }
                }
                Console.WriteLine("Closing Connection");
                sender.Shutdown(SocketShutdown.Receive);
                stream.Close();
                sender.Close();
            }
            TestUtil.StopTimerTicking(0);
            TestUtil.ShowAllTimers();
        }
    }
}
