using Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPFUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ProgressBar Progress;
        public MainWindow()
        {
            InitializeComponent();
            InitilizeSelf();
            ControlWriter wrt = new ControlWriter(this);
            Console.SetOut(wrt);
            Console.SetError(wrt);
            Console.WriteLine("Application Started");
        }

        void InitilizeSelf()
        {
            Progress = this.ProgressBar;
            LogArea.TextChanged += (sender, e) => LogArea.ScrollToEnd();
            //LogArea.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            //LogArea.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        }
        private void ServeButtonClick(object ignore0, RoutedEventArgs ignore1)
        {
            string ip = IPField.Text.Trim();
            string directory = DirectoryField.Text.Trim();
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
            string ip = IPField.Text.Trim();
            string directory = DirectoryField.Text.Trim();
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
        public void Write(string s)
        {
            Application.Current.Dispatcher
                .Invoke((Action)(() => { this.LogArea.Text += s; }));
        }

        public static T GetParentOfType<T>(DependencyObject current)
          where T : DependencyObject
        {
            for (DependencyObject parent = VisualTreeHelper.GetParent(current);
                parent != null;
                parent = VisualTreeHelper.GetParent(parent))
            {
                T result = parent as T;

                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
