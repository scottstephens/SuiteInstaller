using System;
using System.Collections.Generic;
using System.Text;

namespace SuiteInstaller.InstallerLib
{
    public class SourceNotFoundException : Exception
    {
        public string SourceFolder;

        public SourceNotFoundException(string source_folder)
            : base(BuildMessage(source_folder))
        {
            this.SourceFolder = source_folder;
        }

        private static string BuildMessage(string source_folder)
        {
            return $"Could not find {source_folder}";
        }
    }
}
