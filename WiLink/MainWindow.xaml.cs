using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPFUI;
using WPFUI.Core;

namespace WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow instance;
        public static List<float> ItemsProgress = new List<float>();
        public static List<float> ItemsProgressHistory = new List<float>();

        public static long TotalSize;
        public static double ProgressSize;

        private int LastTrackerIndex;

        public MainWindow()
        {
            InitializeComponent();
            InitilizeSelf();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
        }

        internal void SetStatus(string status)
        {
            Dispatcher.Invoke(() => StatusBox.Content = status);
        }

        void InitilizeSelf()
        {
            instance = this;
            Title = "WI-LINK By Eboubaker.B";
        }
        public void SetServing(bool serving)
        {
            Dispatcher.Invoke(() => {
                if (serving)
                {
                    ServeButton.Content = "Cancell";
                    SharedAttributes.ServePending = true;
                    ReceiveButton.IsEnabled = false;
                }
                else
                {
                    SharedAttributes.ServePending = false;
                    ServeButton.Content = "Cancelling";
                    ServeButton.IsEnabled = false;
                }
            });
        }
        public void ConfirmServeCancelled()
        {
            Dispatcher.Invoke(() =>
            {
                ServeButton.Content = "Serve";
                ServeButton.IsEnabled = true;
                ReceiveButton.IsEnabled = true;
                SharedAttributes.ServePending = false;
                StatusBox.Content = "Waiting for Command";
            });
        }

        public void SetReceiving(bool receiving)
        {
            Dispatcher.Invoke(() => {
                if (receiving)
                {
                    ReceiveButton.Content = "Cancell";
                    SharedAttributes.ReceivePending = true;
                    ServeButton.IsEnabled = false;
                }
                else
                {
                    SharedAttributes.ReceivePending = false;
                    ReceiveButton.Content = "Cancelling";
                    ReceiveButton.IsEnabled = false;
                }
            });
        }
        public void ConfirmReceiveCancelled()
        {
            Dispatcher.Invoke(() =>
            {
                ReceiveButton.Content = "Receive";
                ReceiveButton.IsEnabled = true;
                ServeButton.IsEnabled = true;
                SharedAttributes.ReceivePending = false;
                StatusBox.Content = "Waiting for Command";
            });
        }

        private void ServeButtonClick(object ignore0, RoutedEventArgs ignore1)
        {
            if(SharedAttributes.ServePending)
            {
                SetServing(false);
                return;
            }
            string ip = IPField.Text.Trim();
            string directory = DirectoryField.Text.Trim();
            if (directory.StartsWith("\"") && directory.EndsWith("\""))
            {
                directory = directory.Substring(1, directory.Length - 2);
            }
            List<string> directories = new List<string>();
            IPAddress address = null;
            try
            {
                try
                {
                    if (ip.Length != 0)
                    {
                        address = IPAddress.Parse(ip);
                    }
                }
                catch
                {
                    throw new Exception("This IPAddress is not valid\n" + ip);
                }
                if (directory.Length == 0)
                {
                    throw new Exception("No files were given");
                }
                else
                {
                    foreach (string dir in directory.Split(';'))
                    {
                        // throws exception if the directory does not exist
                        File.GetAttributes(dir).HasFlag(FileAttributes.Directory);
                        directories.Add(dir);
                    }
                }
                new Thread(new ThreadStart(() => Sender.SendFiles(address, directories.ToArray()))).Start();
                    
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show(e.Message);
            }
        }
        private void ReceiveButtonClick(object ignore0, RoutedEventArgs ignore1)
        {
            if (SharedAttributes.ReceivePending)
            {
                SetReceiving(false);
                return;
            }
            string ip = IPField.Text.Trim();
            string directory = DirectoryField.Text.Trim();
            if (directory.StartsWith("\"") && directory.EndsWith("\""))
            {
                directory = directory.Substring(1, directory.Length - 2);
            }
            IPAddress address = null;
            try
            {
                try
                {
                    if (ip.Length != 0)
                    {
                        address = IPAddress.Parse(ip);
                    }
                    else
                    {
                        address = null;
                    }
                }
                catch
                {
                    throw new Exception("This IPAddress is not valid\n" + ip);
                }
                if (directory.Length != 0)
                {
                    File.GetAttributes(directory).HasFlag(FileAttributes.Directory);
                }
                else
                {
                    directory = null;
                }
                new Thread(new ThreadStart(() => Receiver.RecieveFiles(directory, address))).Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public void AddItem(Item m)
        {
            this.FileList.Items.Add(new ItemView(m));
        }
        public void SetItemProgress(int id, int value)
        {
            (this.FileList.Items[id] as ItemView).setProgress(value);
        }

        public void ThrowTracker()
        {
            Application.Current.Dispatcher.Invoke((Action)(() => 
            {
                Monitor.Enter(ItemsProgress);
                for (int i = LastTrackerIndex; i < ItemsProgress.Count; i++)
                {
                    float Iprogress = ItemsProgress[i];
                    if (Iprogress == 0)
                    {
                        break; // we passed the last 'currentItem'
                    }
                    if(Iprogress == 1 && ItemsProgressHistory[i] == 1)
                    {
                        LastTrackerIndex = i + 1;
                        continue;
                    }
                    SetItemProgress(i, (int)(Iprogress * 100f));
                    ItemsProgressHistory[i] = Iprogress;
                }

                ProgressNumber.Content = (((int)(ProgressSize / TotalSize * 10000)) / 100f) + "%";
                Monitor.Exit(ItemsProgress);
            }
            ));
        }

    }
}
class ItemView : ListBoxItem
{
    Item Item { get; set; }
    ProgressBar ItemProgress;
    public ItemView(Item itm)
    {
        //< ListBoxItem Height = "28" >
        //    < Grid Height = "29" Width = "485" >
        //        < Label Height = "29" VerticalAlignment = "Top" Content = "File" Margin = "0,0,307,0" />
        //        < ProgressBar Height = "29" Margin = "183,0,0,0" VerticalAlignment = "Top" />
        //    </ Grid >
        //</ ListBoxItem >

        this.Width = MainWindow.instance.FileList.ActualWidth - 50;
        this.Height = 16;
        Item = itm;
        Grid Holder = new Grid();
        Label ItemName = new Label();
        ItemProgress = new ProgressBar();
        

        Holder.Height = this.Height;
        Holder.Width = this.Width;

        ItemName.Content = itm.DisplayName;
        ItemName.Height = Holder.Height;
        ItemName.Margin = new Thickness(10, 0, 0, 0);
        ItemName.Padding = new Thickness(0, 0, 0, 0);
        ItemName.VerticalAlignment = VerticalAlignment.Center;
        ItemName.HorizontalAlignment = HorizontalAlignment.Left;
        ItemName.Width = 500;
        ItemName.FontSize = 9;
        
        ItemProgress.Height = Holder.Height;
        ItemProgress.Width = 460;
        ItemProgress.Padding = new Thickness(0, 0, 0, 0);
        ItemProgress.Margin = new Thickness(0, 0, 10, 0);
        ItemProgress.VerticalAlignment = VerticalAlignment.Center;
        ItemProgress.HorizontalAlignment = HorizontalAlignment.Right;

        Holder.Children.Add(ItemName);
        Holder.Children.Add(ItemProgress);
        this.Content = Holder;
    }
    public void setProgress(int value)
    {
        this.ItemProgress.Value = value;
    }
}