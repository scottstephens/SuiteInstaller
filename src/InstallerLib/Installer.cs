using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SuiteInstaller.InstallerLib
{
    public class Installer
    {
        public string BinarySourceFolder;
        public string ShortcutSourceFolder;
        public string BinaryDestinationFolder;
        public string ShortcutDestinationFolder;

        public Installer(string bsf, string ssf, string bdf, string sdf)
        {
            this.BinarySourceFolder = bsf;
            this.ShortcutSourceFolder = ssf;
            this.BinaryDestinationFolder = bdf;
            this.ShortcutDestinationFolder = sdf;
        }

        private static string EnvironmentVariableSubFolder(string environment_variable, string rel_path)
        {
            var env_path = Environment.GetEnvironmentVariable(environment_variable);
            var result = Path.Combine(env_path, rel_path);
            return result;
        }

        public static readonly Installer Default = new Installer(
            @"\\example.com\ProductionAppData\InHouseAppDeployment",
            @"\\example.com\ProductionAppData\InHouseAppLaunchers",
            EnvironmentVariableSubFolder("LOCALAPPDATA", @"ExampleCo\Apps"),
            EnvironmentVariableSubFolder("APPDATA", @"Microsoft\Windows\Start Menu\Programs\0ExampleCo")
        );

        public void FastNetworkCheck()
        {
            var shortcut_task = Task.Run(this.CheckShortcutSource);
            var binary_task = Task.Run(this.CheckBinarySource);
            if (!Task.WaitAll(new[] { shortcut_task, binary_task }, TimeSpan.FromSeconds(5.0)))
                throw new SourceNotFoundException(this.ShortcutSourceFolder);
            else if (shortcut_task.Exception != null)
                throw new SourceNotFoundException(this.ShortcutSourceFolder);
            else if (binary_task.Exception != null)
                throw new SourceNotFoundException(this.BinarySourceFolder);
        }

        private void CheckShortcutSource()
        {
            if (!Directory.Exists(this.ShortcutSourceFolder))
                throw new SourceNotFoundException(this.ShortcutSourceFolder);
        }

        private void CheckBinarySource()
        {
            if (!Directory.Exists(this.BinarySourceFolder))
                throw new SourceNotFoundException(this.BinarySourceFolder);
        }

        public void UpdateShortcuts()
        {
            if (Directory.Exists(this.ShortcutDestinationFolder))
            {
                var existing_files = Directory.EnumerateFiles(this.ShortcutDestinationFolder);
                foreach (var existing_file in existing_files)
                {
                    var name = Path.GetFileName(existing_file);

                    var src_path = Path.Combine(this.ShortcutSourceFolder, name);

                    if (!File.Exists(src_path))
                        File.Delete(existing_file);
                }
            }

            FileUtils.CopyFolderRecursive(this.ShortcutSourceFolder, this.ShortcutDestinationFolder);
        }

        public void InstallAllApps()
        {
            if (Directory.Exists(this.BinaryDestinationFolder))
            {
                var bin_folders = Directory.EnumerateDirectories(this.BinaryDestinationFolder).ToList();
                foreach (var bin_folder in bin_folders)
                {
                    var folder = FileUtils.GetLeafFolder(bin_folder);
                    var expected_shortcut = Path.Combine(this.ShortcutSourceFolder, $"{folder}.lnk");
                    if (!File.Exists(expected_shortcut))
                        Directory.Delete(bin_folder);
                }
            }
            var shortcuts = Directory.GetFiles(this.ShortcutSourceFolder);
            foreach (var shortcut in shortcuts)
            {
                var folder = Path.GetFileNameWithoutExtension(shortcut);
                if (folder == "Install" || folder == "Remove")
                    continue;

                var source_folder = Path.Combine(this.BinarySourceFolder, folder);
                var dest_folder = Path.Combine(this.BinaryDestinationFolder, folder);
                if (!Directory.Exists(dest_folder))
                    FileUtils.CopyFolderRecursive(source_folder, dest_folder);
            }
        }

        public void Remove()
        {
            if (Directory.Exists(this.BinaryDestinationFolder))
                Directory.Delete(this.BinaryDestinationFolder);
            if (Directory.Exists(this.ShortcutDestinationFolder))
                Directory.Delete(this.ShortcutDestinationFolder);
        }

        public void Install()
        {
            this.UpdateShortcuts();
            this.InstallAllApps();
        }

        public bool UpdateAvailable(string folder_name, string exe_name)
        {
            var binary_source = Path.Combine(this.BinarySourceFolder, folder_name, exe_name);
            var binary_dest = Path.Combine(this.BinaryDestinationFolder, folder_name, exe_name);

            var source_timestamp = File.GetLastWriteTimeUtc(binary_source);
            var dest_timestamp = File.GetLastWriteTimeUtc(binary_dest);

            return source_timestamp > dest_timestamp;
        }

        public void UpdateSingle(string folder_name, string exe_name)
        {
            var source_folder = Path.Combine(this.BinarySourceFolder, folder_name);
            var dest_folder = Path.Combine(this.BinaryDestinationFolder, folder_name);
            
            var source_binary = Path.Combine(source_folder, exe_name);
            var dest_binary = Path.Combine(source_folder, exe_name);
            var source_timestamp = File.GetLastWriteTimeUtc(source_binary);
            var dest_timestamp = File.GetLastWriteTimeUtc(dest_binary);

            if (source_timestamp > dest_timestamp)
                FileUtils.CopyFolderRecursive(source_folder, dest_folder);
        }

        public void UpdateAndStart(string[] args)
        {
            var folder = args[1];
            var exe = args[2];
            if (args.Length > 3)
            {
                var pid = Int32.Parse(args[3]);
                this.WaitForProcessToClose(pid);
            }

            this.UpdateSingle(folder, exe);
            this.Launch(folder, exe);
        }

        public void WaitForProcessToClose(int pid)
        {
            var p = Process.GetProcessById(pid);
            p.WaitForExit();
        }

        public void CheckForUpdateAndRestartIfNecessary()
        {
            var entry_assembly = Assembly.GetEntryAssembly();
            var folder = FileUtils.GetLeafFolder(Path.GetDirectoryName(entry_assembly.Location));
            var exe = Path.GetFileName(entry_assembly.Location);

            if (entry_assembly.Location.StartsWith(this.BinaryDestinationFolder))
            {
                if (UpdateAvailable(folder, exe))
                {
                    var installer_path = Path.Combine(this.BinarySourceFolder, "Installer", "Installer.exe");
                    var arguments = $"UpdateAndStart {folder} {exe} {Process.GetCurrentProcess().Id}";
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(installer_path, arguments);
                    p.Start();
                    Environment.Exit(0);
                }
            }
        }

        public void Launch(string folder_name, string exe_name)
        {
            var p = new Process();
            var app_path = Path.Combine(this.BinaryDestinationFolder, folder_name, exe_name);
            p.StartInfo = new ProcessStartInfo(app_path);
            p.Start();
        }
    }
}