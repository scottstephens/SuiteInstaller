#if NETSTANDARD
using System;
using System.Collections.Generic;
using System.Text;

namespace SuiteInstaller.InstallerLib
{
    public class ShortcutCreator : IShortcutCreator
    {
        public static readonly ShortcutCreator Instance = new ShortcutCreator();

        public void Create(string shortcut_location, Shortcut shortcut)
        {
            throw new NotImplementedException("Only supported in .NET Core version of library.");
        }

        public Shortcut Read(string shortcut_location)
        {
            throw new NotImplementedException("Only supported in .NET Core version of library.");
        }
    }
}
#endif
