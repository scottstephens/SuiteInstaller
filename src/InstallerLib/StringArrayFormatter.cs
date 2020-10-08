using System;
using System.Collections.Generic;
using System.Text;

namespace ScottStephens.SuiteInstaller.InstallerLib
{
    public class StringArrayFormatter
    {
        public readonly string[] Content;

        public StringArrayFormatter(string[] content)
        {
            this.Content = content;
        }

        public override string ToString()
        {
            return String.Join(",", this.Content);
        }
    }
}


