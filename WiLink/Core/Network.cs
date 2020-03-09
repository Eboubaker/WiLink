using Microsoft.Win32;
using NativeWifi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
    class Network
    {
        #region Sockets


        public static bool SocketConnectWithTimeout(out Socket socket, IPEndPoint endpoint, int timeout)
        {

            DateTime breakOn = DateTime.Now.AddSeconds(30);
            bool connected = false;
            while (!connected && DateTime.Now.CompareTo(breakOn) < 0)
            {
                try
                {
                    socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    IAsyncResult result = socket.BeginConnect(endpoint, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
                    if (socket.Connected)
                    {
                        socket.EndConnect(result);
                        return true;
                    }
                    Thread.Sleep(2000);
                }
                catch { }
                RefereshNetwork();
            }
            socket = null;
            return false;
        }
        public static bool SocketAcceptWithTimeout(out Socket socket, Socket server, int timeout)
        {
            
            IAsyncResult result = server.BeginAccept(null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            socket = server.EndAccept(result);
            if (socket.Connected)
            {
                return true;
            }
            else
            {
                socket = null;
                return false;
            }
        }
        public static bool SocketAcceptWithTimeout(out Socket socket, IPEndPoint endpoint, int timeout)
        {
            Socket server = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(endpoint);
            server.Listen(1);
            IAsyncResult result = server.BeginAccept(null, null);
            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            socket = server.EndAccept(result);
            server.Close();
            if (socket.Connected)
            {
                return true;
            }
            else
            {
                socket = null;
                return false;
            }
        }
        #endregion

        #region Address Handlers
        public static string GetNetworkMAC()
        {
            string[] lines;
            Utility.Execv("netsh", "wlan show hostednetwork", out lines);
            foreach (string line in lines)
            {
                if (line.Contains("BSSID"))
                {
                    return line.Substring(line.IndexOf(":") + 1).Trim().Replace(":", "").ToUpper();
                }
            }
            return string.Empty;
        }

        public static IPAddress GetNetworkAddress()
        {
            string targetMAC = GetNetworkMAC();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.GetPhysicalAddress().ToString() == targetMAC)
                {
                    foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return IPAddress.Parse(ip.Address.ToString());
                        }
                    }
                }
            }
            return null;
        }
        public static IPAddress GetDefaultNetworkAddress()
        {
            using (RegistryKey wlanKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\WlanSvc\\Parameters\\EapolKeyIpAddress"))
            {
                if (wlanKey != null)
                {
                    object keyValue = wlanKey.GetValue("LocalAddress");
                    if (keyValue != null)
                    {
                        return IPAddress.Parse(keyValue.ToString());
                    }
                }
                return null;
            }
        }
        #endregion

        #region HostedNetwork Handlers
        public static bool StartHostedNetwork(out IPAddress address)
        {
            address = null;
            Process ignore;
            if (!IsHostedNetworkSupported())
                return false;
            if (!StopHostedNetwork())
                return false;
            //Thread.Sleep(1000);
            if (!Utility.Exec("netsh", "wlan start hostednetwork", out ignore))
                return false;
            //Thread.Sleep(1000);
            IPAddress temp = GetNetworkAddress();
            if (!SetHostedNetwork(temp.ToString()))
                return false;
            //Thread.Sleep(1000);
            if (!StopHostedNetwork())
                return false;
            //Thread.Sleep(1000);
            if (!Utility.Exec("netsh", "wlan start hostednetwork", out ignore))
                return false;
            //Thread.Sleep(1000);
            address = temp;
            return true;
        }
        /// <summary>
        /// sets the name and the password of the hostednetwork using the given ip
        /// </summary>
        /// <param name="ip"></param>
        public static bool SetHostedNetwork(string ip)
        {
            string password = ip.Substring("192.168.".Length).LocalEncodeBase64();
            while (password.Length < 8)
                password = password + '_';
            string ssid = "WI_" + password;
            Process p;
            return Utility.Exec("netsh", String.Format("wlan set hostednetwork mode=allow ssid={0} key={1}", ssid, password), out p);
        }
        public static bool StopHostedNetwork()
        {
            Process p;
            return Utility.Exec("netsh", "wlan stop hostednetwork", out p);
        }
        public static bool IsHostedNetworkSupported()
        {
            string[] lines;
            if(!Utility.Execv("netsh", "wlan show drivers", out lines))
            {
                return false;
            }
            foreach (string line in lines)
            {
                if (   line.Contains("Hosted network supported")
                    && line.Split(':')[1].Trim() == "Yes")
                    return true;
            }
            return false;
        }
        #endregion

        #region General Networking
        public static void RefereshNetwork()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "wirefnet.exe";
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process p = Process.Start(processStartInfo) ;
            p.WaitForExit();
            
        }
        public static bool ConnectToAvailableWiLinkNetwork(out IPAddress address)
        {
            DateTime stop = DateTime.Now.AddSeconds(30);
            WlanClient client = new WlanClient();
            while (stop.CompareTo(DateTime.Now) > 0)
            {
                foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
                {
                    foreach (Wlan.WlanBssEntry network in wlanIface.GetNetworkBssList())
                    {
                        List<byte> bytes = new List<byte>();
                        foreach (byte b in network.dot11Ssid.SSID)
                        {
                            if (b == 0)
                                break;
                            bytes.Add(b);
                        }
                        string ssid = Encoding.UTF8.GetString(bytes.ToArray());
                        if (ssid.StartsWith("WI_"))
                        {
                            address = ConnectToNetwork(ssid, ssid.Substring(ssid.IndexOf("_") + 1));
                            return true;
                        }
                    }
                }
                RefereshNetwork();
                Thread.Sleep(1000);
            }
            address = null;
            return false;
        }


        private static IPAddress ConnectToNetwork(string ssid, string key)
        {
            WlanClient client = new WlanClient();
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                string hash = GetSSIDHash(ssid);
                string profileXml = "<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{2}</keyMaterial></sharedKey></security></MSM><MacRandomization xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v3\"></MacRandomization></WLANProfile>";
                wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, String.Format(profileXml, ssid, hash, key), true);
                wlanIface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, ssid);
                return IPAddress.Parse("192.168."+key.LocalDecodeBase64());
            }
            return null;
        }
        public static string GetSSIDHash(string ssid)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "powershell.exe";
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.Arguments = "(\\\"" + ssid + "\\\".ToCharArray() |foreach-object {'{0:X}' -f ([int]$_)}) -join ''";
            processStartInfo.UseShellExecute = false;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process p = Process.Start(processStartInfo);
            p.WaitForExit();
            return p.StandardOutput.ReadToEnd().Trim();
        }
        #endregion

    }
}
