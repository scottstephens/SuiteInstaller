using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;

namespace SuiteInstaller.InstallerLib
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            try
            {
                var installer = getDefaultInstaller();
                installer.InitializeLog4Net();

                log.DebugFormat("Main({0})", new StringArrayFormatter(args));

                if (args.Length == 0)
                {
                    Console.WriteLine("Pick one of the following options:");
                    Console.WriteLine("Install");
                    Console.WriteLine("Remove");
                    Console.WriteLine("Quit");
                    Console.WriteLine();
                    var r = Console.ReadLine();
                    if (r.ToLower() == "quit")
                        return;
                    else
                        args = new string[] { r };
                }

                installer.FastNetworkCheck();
                if (args[0] == "UpdateShortcuts")
                {
                    installer.UpdateShortcuts();
                }
                else if (args[0] == "Install")
                {
                    installer.Install();
                }
                else if (args[0] == "Remove")
                {
                    installer.Remove();
                }
                else if (args[0] == "UpdateAndStart")
                {
                    installer.UpdateAndStart(args);
                }
                else if (args[0] == "UpdateAndClose")
                {
                    installer.CheckForUpdateAndClose(args[1], args[2]);
                }
                else
                {
                    throw new Exception($"Invalid argument {args[0]}");
                }
            } 
            catch (SourceNotFoundException e)
            {
                log.Error("Error finding source folder", e);
                Console.WriteLine($"Error finding source folder {e.SourceFolder}");
                Console.WriteLine();
                Console.WriteLine("If the source folder is on a network drive, try opening it in Windows Explorer ");
                Console.WriteLine("first. If it's password protected, doing so will give you the chance to enter ");
                Console.WriteLine("your user name and password.");
                Console.WriteLine();
                Console.WriteLine("If you need to use a VPN to access the folder, double check that it's turned on.");
                Console.WriteLine();
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                log.Error("Unexpected error", e);
                Console.WriteLine($"Unexpected error. Contact your software support team.");
                Console.WriteLine();
                Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
            }
        }

        private static Installer getDefaultInstaller()
        {
            var entry_assembly_path = Assembly.GetEntryAssembly().Location;
            var entry_assembly_folder = Path.GetDirectoryName(entry_assembly_path);
            var default_config_path = Path.Combine(entry_assembly_folder, "..", "SuiteInstallerConfig.json");
            return new Installer(default_config_path);
        }
    }
}
