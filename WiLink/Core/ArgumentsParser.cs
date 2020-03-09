using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Core
{
    class ArgumentsParser
    {
        // serve c:\somefile\another "c:\other files here too\file.mp4" -i 192.168.0.1
        // get -o "c:\somefile\another" -i 192.168.0.1
        public static void StartWithArgs(string[] args)
        {
            if (args.Length == 0 || args[0] == "help")
            {
                ShowSyntax();
                Utility.Exit();
            }

            IPAddress ip = null;
            string outdir = null;

            if (args[0] == "serve")
            {
                if(args.Length < 2)
                {
                    Utility.Exit("No input files were given.");
                }
                List<string> dirs = new List<string>();
                int i = 1;
                while(i < args.Length && args[i].Length != 2 && args[i][0] != '-')
                {
                    try
                    {
                        if (!Path.IsPathRooted(args[i]))
                        {
                            throw new Exception();
                        }
                        dirs.Add(Path.GetFullPath(args[i]));
                    } catch {
                        Utility.Exit(String.Format("Directory {0} is not a correct directory.", args[i]));
                    }
                    i++;
                }
                if (dirs.Count == 0)
                {
                    Utility.Exit("No input files were given.");
                }
                if(i < args.Length && args[i] == "-i")
                {
                    if(!IPAddress.TryParse(args[i + 1], out ip))
                    {
                        Utility.Exit("The IP address is not correct.");
                    }
                }
                Sender.SendFiles(ip, dirs.ToArray());
            }
            else if (args[0] == "get")
            {
                if(args.Length > 1)
                {
                    for(int i = 1; i < args.Length; i++)
                    {
                        if (args[i] == "-o")
                        {
                            try
                            {
                                if (!Path.IsPathRooted(args[i+1]))
                                {
                                    throw new Exception();
                                }
                                outdir = Path.GetFullPath(args[i+1]);
                            }
                            catch
                            {
                                Utility.Exit("Output directory Name is not correct.");
                            }
                        }else if(args[i] == "-i")
                        {
                            if (!IPAddress.TryParse(args[i + 1], out ip))
                            {
                                Utility.Exit("The IP address is not correct.");
                            }
                        }
                    }
                    Receiver.RecieveFiles(outdir, ip);
                }
                else
                {
                    Receiver.RecieveFiles(null, null);
                }
            }
            else
            {
                Utility.Exit("Wrong Syntax use help to show help.");
            }
        }

        private static void ShowSyntax()
        {
            p("WiLink CommandLine");
            p("Usage: wilink.exe serve <directory list> [-i <IP address>]");
            p("Description: Makes this machine as a file server for other devices");
            p("   Parameters:");
            p("     <directory list>: on or more files or directories to serve");
            p("     -i: (Optional) Binds the server to the specifed IP adress");
            p("Note: If no IP address is specified then the program will try to create\n      a new sharing network and binds itself to that network");
            p("\n************************\n");
            p("Usage: wilink.exe get [-o <output directory>] [-i <IP address>]");
            p("Description: Makes this machine acts as a receiver\n             which can receive files from any WiLink file server");
            p("   Parameters:");
            p("     -o: (Optional) sets the output folder");
            p("     -i: (Optional) Connects to the specifed IP adress of the wilink server");
            p("Note: If no IP address is specified then the program will try to search for\n      any WiLink networks and connects to them");
            p("Note: If no output directory is specified then the files will be\n      put in the directory of the program");
            p("\n************************\n");
            p("Program Made By Eboubaker Bekkouche [email: eboubaker.bekkouche@gmail.com | github: @ZOLDIK0]");
        }
        private static void p(string s="")
        {
            Console.WriteLine(s);
        }

        public static string[] getArgs(string inlineArgs)
        {
            List<string> args = new List<string>();
            int index = 0;
            bool encapsule = false;
            foreach(char c in inlineArgs.ToCharArray())
            {
                if(c == ' ' && !encapsule)
                {
                    index++;
                }else if(c == '"')
                {
                    encapsule = !encapsule;
                    continue;
                }
                else
                {
                    if(index == args.Count)
                    {
                        args.Add(c.ToString());
                    }
                    else
                    {
                        args[index] = args[index] + c;
                    }
                }
            }
            return args.ToArray();
        }
    }
}
