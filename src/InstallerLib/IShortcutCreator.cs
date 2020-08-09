using System;
using System.Collections.Generic;
using System.Text;

namespace SuiteInstaller.InstallerLib
{
    public interface IShortcutCreator
    {
        void Create(string shortcut_location, Shortcut shortcut);
        Shortcut Read(string shortcut_location);
    }

    public enum ShortcutWindowStyle : int
    {
        NormalWindow = 1,
        Maximized = 3,
        Minimized = 7,        
    };

    public class Shortcut
    {
        public string Arguments { get; set; }
        public string Description { get; set; }
        public string Hotkey { get; set; }
        public string IconLocation { get; set; }
        public string TargetPath { get; set; }
        public ShortcutWindowStyle? WindowStyle { get; set; }
        public string WorkingDirectory { get; set; }

        public Shortcut()
        {
            this.Arguments = "";
            this.Description = "";
            this.Hotkey = "";
            this.IconLocation = ",0";
            this.WindowStyle = ShortcutWindowStyle.NormalWindow;
            this.WorkingDirectory = "";
        }
    }
}
