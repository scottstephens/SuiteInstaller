#if NETCOREAPP3_1
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IWshRuntimeLibrary;

namespace SuiteInstaller.InstallerLib
{
    public class ShortcutCreator : IShortcutCreator
    {
        public static readonly ShortcutCreator Instance = new ShortcutCreator();

        public void Create(string shortcut_location, Shortcut shortcut)
        {
            if (!shortcut_location.EndsWith(".lnk"))
                throw new ArgumentException($"shortcut_location must end with .lnk; is {shortcut_location}");

            if (shortcut.TargetPath == null)
                throw new ArgumentException("shortcut.TargetPath is null");

            WshShell shell = new WshShell();
            var wsh_shortcut = (IWshShortcut)shell.CreateShortcut(shortcut_location);

            wsh_shortcut.TargetPath = shortcut.TargetPath;

            if (shortcut.Description != null)
                wsh_shortcut.Description = shortcut.Description;
            if (shortcut.Hotkey != null)
                wsh_shortcut.Hotkey = shortcut.Hotkey;
            if (shortcut.IconLocation != null)
                wsh_shortcut.IconLocation = shortcut.IconLocation;
            if (shortcut.WindowStyle.HasValue)
                wsh_shortcut.WindowStyle = (int)shortcut.WindowStyle.Value;
            if (shortcut.WorkingDirectory != null)
                wsh_shortcut.WorkingDirectory = shortcut.WorkingDirectory;

            wsh_shortcut.Save();
        }

        public Shortcut Read(string shortcut_location)
        {
            WshShell shell = new WshShell();
            var wsh_shortcut = (IWshShortcut)shell.CreateShortcut(shortcut_location);
            var output = new Shortcut();
            //output.
            return null;
        }
    }

    

    //class WshShortcut : IWshShortcut
    //{
    //    public void Load(string PathLink)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Save()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public string FullName => throw new NotImplementedException();

    //    public string Arguments { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string Hotkey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string IconLocation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string RelativePath { set => throw new NotImplementedException(); }
    //    public string TargetPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public int WindowStyle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public string WorkingDirectory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //}
}
#endif