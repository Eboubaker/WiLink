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
    class Receiver
    {
        private static void err(string message)
        {
            MainWindow.instance.SetReceiving(false);
            MessageBox.Show(message);
        }
        public static void RecieveFiles(string outputDirectory, IPAddress ip = null)
        {
            try
            {
                MainWindow.instance.SetReceiving(true);
                MainWindow.instance.SetStatus("Waiting for Available WiLink Networks");
                if (ip == null && !Network.ConnectToAvailableWiLinkNetwork(out ip) && SharedAttributes.ReceivePending)
                {
                    throw new Exception("No Network Found");
                }
                if (outputDirectory == null)
                    outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Received");

                MainWindow.instance.SetStatus("Waiting for Sender Signal");
                if(!AwaitSender(ip))
                {
                    if(!SharedAttributes.ReceivePending)
                    {
                        throw new Exception("NULL");
                    }
                    else
                    {
                        throw new Exception("No Network Found");
                    }
                }
                MainWindow.instance.SetStatus("Obtaining Meta info");
                MetaInfo meta = MetaInfo.ReceiveMetas(ip);
                MainWindow.TotalSize = meta.Items.TotalSize;
                MainWindow.instance.SetStatus("Starting Receive Operations");
                ReceiveOperation.items = meta.Items.Items;
                MainWindow.instance.Dispatcher.Invoke((Action)(() =>
                {
                    foreach (Item itm in meta.Items.Items)
                    {
                        MainWindow.instance.AddItem(itm);
                    }
                }));
                IPEndPoint endPoint = new IPEndPoint(ip, meta.Ports[0]);
                string outPath = Utility.PathCombine(outputDirectory, meta.SenderName);
                Thread t1 = new Thread(new ThreadStart(() => ReceiveOperation.Receive(endPoint, outPath)));
                t1.Start();
                t1.Join();
                
            }
            catch(Exception e)
            {
                if(e.Message != "NULL")
                {
                    err(e.Message);
                }
            }
            finally
            {
                if (SharedAttributes.ReceivePending)
                {
                    MainWindow.instance.SetReceiving(false);
                }
                MainWindow.instance.ConfirmReceiveCancelled();
            }
        }

        public static bool AwaitSender(IPAddress addrr)
        {
            IPEndPoint senderEndPoint = new IPEndPoint(addrr, Constants.Signal_PORT);
            Socket s = new Socket(SocketType.Stream, ProtocolType.Tcp);
            DateTime timeoutTime = DateTime.Now.AddSeconds(60);
            do
            {
                try
                {
                    s.Connect(senderEndPoint);
                    s.Close();
                    return true;
                }
                catch { }
                Thread.Sleep(500);
            } while (DateTime.Now < timeoutTime && SharedAttributes.ReceivePending);
            return false;
        }
    }
}
