using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SuiteInstaller.Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            SuiteInstaller.InstallerLib.Installer.InitializeLog4Net();
            SuiteInstaller.InstallerLib.Program.Main(args);
        }
    }
}
