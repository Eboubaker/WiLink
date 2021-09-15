using NativeWifi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFUI;
using WPFUI.Core;

namespace Core
{
    
    class Sender
    {
        private static void err(string message)
        {
            MainWindow.instance.SetServing(false);
            MessageBox.Show(message);
        }
        public static void SendFiles(IPAddress ip, params string[] paths)
        {
            try
            {
                MainWindow.instance.SetServing(true);
                if (ip == null)
                {
                    MainWindow.instance.SetStatus("Starting WiLink Network");
                    if (!Network.StartHostedNetwork(out ip))
                    {
                        throw new IOException("Couldn't Start WiLink Network");
                    }
                }
                MainWindow.instance.SetStatus("Indexing Files");
                List<FileItem> filelist = new List<FileItem>();
                foreach (string p in paths)
                {
                    filelist.AddRange(Utility.GetFiles(p));
                }
                TransferItems items = new TransferItems();
                for (int i = 0; i < filelist.Count; i++)
                {
                    filelist[i].Id = i;
                    filelist[i].DisplayName = filelist[i].Name;
                    items.Add(i, filelist[i]);
                }
                MainWindow.instance.Dispatcher.Invoke((Action)(() =>
                {
                    foreach (Item itm in items.Items)
                    {
                        MainWindow.instance.AddItem(itm);
                    }
                }));
                MainWindow.instance.SetStatus("Waiting for Receiver Signal...");
                if (!AwaitReceiver(ip))
                {
                    if (!SharedAttributes.ServePending)
                    {
                        throw new Exception("NULL");
                    }
                    else
                    {
                        throw new Exception("No Receivers Found, Timeout 60 seconds");
                    }
                }
                MainWindow.instance.SetStatus("Sending Meta Info");
                MainWindow.TotalSize = items.TotalSize;
                MetaInfo metainfo = new MetaInfo(items);
                metainfo.SendMetas(ip);
                MainWindow.instance.SetStatus("Starting Send Operations");

                SendOperation.items = new Dictionary<int, Item>();
                foreach (Item m in metainfo.Items.Items)
                {
                    SendOperation.items.Add(m.Id, m);
                }
                IPEndPoint ReceiverEndPoint = new IPEndPoint(ip, metainfo.Ports[0]);
                Thread t1 = new Thread(new ThreadStart(() => SendOperation.Send(ReceiverEndPoint)));
                t1.Start();
                t1.Join();
            }
            catch(Exception e)
            {
                if (e.Message != "NULL")
                {
                    err(e.Message);
                }
            }
            finally
            {
                if (SharedAttributes.ServePending)
                {
                    MainWindow.instance.SetServing(false);
                }
                MainWindow.instance.ConfirmServeCancelled();
            }
        } 
        public static bool AwaitReceiver(IPAddress addrr)
        {
            IPEndPoint senderEndPoint = new IPEndPoint(addrr, Constants.Signal_PORT);
            Socket server = new Socket(SocketType.Stream, ProtocolType.Tcp);
            DateTime timeoutTime = DateTime.Now.AddSeconds(60);
            do
            {
                try
                {
                    server.Bind(senderEndPoint);
                    server.Listen(1);
                    Socket reciver = server.Accept();
                    server.Close();
                    reciver.Close();
                    return true;
                }
                catch (Exception e){ }
                Thread.Sleep(500);
            } while (DateTime.Now < timeoutTime && SharedAttributes.ServePending);
            return false;
        }
    }
    
}
