using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core
{
    static class Utility
    {
        #region Process Handlers
        public static bool Exec(string exe, string command, out Process process)
        {
            ProcessStartInfo stinfo = new ProcessStartInfo();
            stinfo.RedirectStandardInput = true;
            stinfo.RedirectStandardOutput = true;
            stinfo.WindowStyle = ProcessWindowStyle.Hidden;
            stinfo.FileName = exe;
            stinfo.Arguments = command;
            stinfo.CreateNoWindow = true;
            stinfo.UseShellExecute = false;
            process = Process.Start(stinfo);
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        public static void AsyncExec(string command)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd.exe");
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            Process process = Process.Start(processStartInfo);
            process.StandardInput.WriteLine(command);
            process.StandardInput.Close();
        }
        public static bool Execv(string exe, string command, out string[] output)
        {
            Process process;
            if (!Exec(exe, command, out process))
            {
                output = null;
                return false;
            }
            output = process.StandardOutput.ReadToEnd().Split("\r\n".ToCharArray());
            return true;
        }

        #endregion

        #region File Handlers
        public static List<FileItem> GetFiles(string dir, string localPath = "")
        {

            List<FileItem> ret = new List<FileItem>();
            bool added = false;
            bool isdir = File.GetAttributes(dir).HasFlag(FileAttributes.Directory);
            if (isdir)
            {
                string[] dirs = Directory.GetFiles(dir);
                added = dirs.Length > 0;
                foreach (string file in dirs)
                {
                    ret.Add(new FileItem(file, localPath));
                }
                dirs = Directory.GetDirectories(dir);
                added = !added ? dirs.Length > 0 : true;
                foreach (string subdir in dirs)
                {
                    ret.AddRange(GetFiles(subdir, Path.Combine(localPath, Path.GetFileName(subdir))));
                }
            }
            if (!added)
            {
                ret.Add(new FileItem(dir, localPath, isdir));
            }
            return ret;
        }
        public static string PathCombine(string p1, string p2, string p3)
        {
            return Path.Combine(p1, Path.IsPathRooted(p2) ? p2.Substring(1) : p2, p3);
        }
        public static string PathCombine(string p1, string p2)
        {
            return Path.Combine(p1, Path.IsPathRooted(p2) ? p2.Substring(1) : p2);
        }

        #endregion

        #region Environment Handlers
        public static void Exit(string message = "", int code = 1)
        {
            Console.WriteLine(message);
            Console.WriteLine("\nPress Any Key to Close...");
            Console.ReadKey();
            Environment.Exit(code);
        }

        #endregion

        #region Tools
        /// <summary>
        /// Local base64 encoding changes all '=' characters to '-' after conferting
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string LocalEncodeBase64(this String text)
        {
            if (text == null) return null;
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes).Replace("=", "-");
            
        }
        /// <summary>
        /// Local base64 decoding changes all '-' characters to '=' before converting
        /// </summary>
        /// <param name="encodedText"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string LocalDecodeBase64(this string encodedText)
        {
            if (encodedText == null) return null;
            byte[] bytes = Convert.FromBase64String(encodedText.Replace("-", "="));
            return Encoding.UTF8.GetString(bytes);
        }

        public static int NthIndexOf(this string target, string value, int n)
        {
            Match m = Regex.Match(target, "((" + Regex.Escape(value) + ").*?){" + n + "}");
            if (m.Success)
                return m.Groups[2].Captures[n - 1].Index;
            else
                return -1;
        }
        #endregion
    }
}
