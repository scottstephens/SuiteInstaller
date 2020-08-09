using System;
using System.Collections.Generic;
using System.Text;

namespace SuiteInstaller.InstallerLib
{
    public class Config
    {
        public string BinarySource { get; set; }
        public string IconSource { get; set; }
        public string LocalAppDataFolder { get; set; }
        public string StartMenuFolder { get; set; }
        public List<App> Apps { get; set; }
    }

    public class App
    {
        public string Name { get; set; }
        public string Folder { get; set; }
        public string Exe { get; set; }
        public string Arguments { get; set; }
        public bool CopyLocal { get; set; }
        public string IconFile { get; set; }
        public int? IconIndex { get; set; }
    }
}
