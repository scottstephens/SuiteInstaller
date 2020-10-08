using System;
using System.Collections.Generic;
using System.Text;
using log4net;

namespace SuiteInstaller.InstallerLib
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            log.DebugFormat("Main({0})", new StringArrayFormatter(args));
            try
            {
                Installer.Default.FastNetworkCheck();
                if (args[0] == "UpdateShortcuts")
                {
                    Installer.Default.UpdateShortcuts();
                }
                else if (args[0] == "Install")
                {
                    Installer.Default.Install();
                }
                else if (args[0] == "Remove")
                {
                    Installer.Default.Remove();
                }
                else if (args[0] == "UpdateAndStart")
                {
                    Installer.Default.UpdateAndStart(args);
                }
                else if (args[0] == "UpdateAndClose")
                {
                    Installer.Default.CheckForUpdateAndClose(args[1], args[2]);
                }
                else
                {
                    throw new Exception($"Invalid argument {args[0]}");
                    //ShortcutTest.Run();
                    //DeserializationTest.Run();
                }
            } 
            catch (SourceNotFoundException e)
            {
                log.Error("Error finding source folder", e);
                Console.WriteLine($"Error finding source folder {e.SourceFolder}");
                Console.WriteLine();
                Console.WriteLine("This is most likely because the program can't find a necessary network share.");
                Console.WriteLine("If you are not at the office, first make sure you are signed into the VPN.");
                Console.WriteLine("Next, navigate to the folder indicated above in Windows Explorer.");
                Console.WriteLine("If you are prompted for a user name and password, use the credentials you use ");
                Console.WriteLine("for logging into your office computer.");
                Console.WriteLine("After that, try again.");
                Console.WriteLine();
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                log.Error("Unexpected error", e);
                Console.WriteLine($"Unexpected error. Show Scott.");
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
    }
}
