using Newtonsoft.Json;
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
        private Config Config;

        public Installer(string config_file)
        {
            this.Config = ParseConfig(config_file);
        }

        private static Config ParseConfig(string path)
        {
            var config_text = File.ReadAllText(path);
            var escaped_config = config_text.Replace(@"\", @"\\");
            var config = JsonConvert.DeserializeObject<Config>(escaped_config);
            return config;
        }

        private static string EnvironmentVariableSubFolder(string environment_variable, string rel_path)
        {
            var env_path = Environment.GetEnvironmentVariable(environment_variable);
            var result = Path.Combine(env_path, rel_path);
            return result;
        }

        private static string EnvironmentVariableSubFolder(string environment_variable, string rel_path1, string rel_path2)
        {
            var env_path = Environment.GetEnvironmentVariable(environment_variable);
            var result = Path.Combine(env_path, rel_path1, rel_path2);
            return result;
        }

        public static readonly Installer Default = new Installer(
            @"\\example.com\ProductionAppData\InHouseAppDeployment\ExampleCoApps.json"
        );

        public void FastNetworkCheck()
        {
            var icon_task = Task.Run(this.CheckIconSource);
            var binary_task = Task.Run(this.CheckBinarySource);
            if (!Task.WaitAll(new[] { icon_task, binary_task }, TimeSpan.FromSeconds(5.0)))
                throw new SourceNotFoundException(this.Config.IconSource);
            else if (icon_task.Exception != null)
                throw new SourceNotFoundException(this.Config.IconSource);
            else if (binary_task.Exception != null)
                throw new SourceNotFoundException(this.Config.BinarySource);
        }

        private void CheckIconSource()
        {
            if (!Directory.Exists(this.Config.IconSource))
                throw new SourceNotFoundException(this.Config.IconSource);
        }

        private void CheckBinarySource()
        {
            if (!Directory.Exists(this.Config.BinarySource))
                throw new SourceNotFoundException(this.Config.BinarySource);
        }

        public void UpdateIcons()
        {
            this.PurgeOutOfDateIcons();
            this.CreateMissingIcons();
        }

        private string getLocalIconFolder()
        {
            return EnvironmentVariableSubFolder("APPDATA", this.Config.LocalAppDataFolder, "Icons");
        }

        private void PurgeOutOfDateIcons()
        {
            var expected = this.Config.Apps
                .Where(x => !x.CopyLocal)
                .Select(x => IconNameFromShortcutIconSpec(x.IconFile))
                .ToDictionary(x => x);

            var local_icon_folder = this.getLocalIconFolder();
            if (Directory.Exists(local_icon_folder))
            {
                var existing = Directory.GetFiles(local_icon_folder);
                foreach (var file in existing)
                {
                    var name = Path.GetFileName(file);
                    if (!expected.TryGetValue(name, out var throwaway))
                        File.Delete(file);
                }
            }
        }

        private static string IconNameFromShortcutIconSpec(string spec)
        {
            var parts = spec.Split(',');
            return Path.GetFileName(parts[0]);
        }

        private static string IconPathFromShortcutIconSpec(string spec)
        {
            var parts = spec.Split(',');
            return parts[0];
        }

        private void CreateMissingIcons()
        {
            var needed = this.Config.Apps
                .Where(x => !x.CopyLocal && x.IconFile != null && IconNameFromShortcutIconSpec(x.IconFile) != "")
                .ToList();

            var local_icon_path = this.getLocalIconFolder();
            if (!Directory.Exists(local_icon_path))
                Directory.CreateDirectory(local_icon_path);

            var existing = Directory.GetFiles(local_icon_path).ToDictionary(x => Path.GetFileName(x));
            foreach (var n in needed)
            {
                var name = IconNameFromShortcutIconSpec(n.IconFile);
                var local_path = Path.Combine(local_icon_path, name);
                var source_path = Path.Combine(this.Config.IconSource, name);
                if (existing.ContainsKey(name))
                {
                    var local_ts = File.GetLastWriteTimeUtc(local_path);
                    var source_ts = File.GetLastWriteTimeUtc(source_path);
                    if (local_ts != source_ts)
                        File.Copy(source_path, local_path, overwrite:true);
                }
                else
                {
                    File.Copy(source_path, local_path);
                }
            }
        }

        private string getStartMenuFolder()
        {
            return EnvironmentVariableSubFolder(
                "APPDATA", 
                @"Microsoft\Windows\Start Menu\Programs",
                this.Config.StartMenuFolder
            );
        }

        public void UpdateShortcuts()
        {
            this.PurgeOutOfDateShortcuts();
            this.CreateMissingShortcuts();
        }

        private void PurgeOutOfDateShortcuts()
        {
            var start_menu = this.getStartMenuFolder();
            var expected = this.Config.Apps.ToDictionary(x => x.Name);
            if (Directory.Exists(start_menu))
            {
                var existing_files = Directory.EnumerateFiles(start_menu);
                foreach (var existing_file in existing_files)
                {
                    var name = Path.GetFileNameWithoutExtension(existing_file);

                    if (expected.TryGetValue(name, out App expected_app))
                    {
                        if (!ExpectedShortcut(existing_file, expected_app))
                            File.Delete(existing_file);
                    }
                    else
                    {
                        File.Delete(existing_file);
                    }
                }
            }
        }

        private void CreateMissingShortcuts()
        {
            var start_menu = this.getStartMenuFolder();
            if (!Directory.Exists(start_menu))
                Directory.CreateDirectory(start_menu);
            var existing = Directory.GetFiles(start_menu).ToDictionary(x => Path.GetFileNameWithoutExtension(x));
            foreach (var app in this.Config.Apps)
            {
                if (!existing.ContainsKey(app.Name))
                    CreateShortcut(app);
            }
        }

        private bool ExpectedShortcut(string shortcut_path, App app)
        {
            var expected = this.BuildShortcut(app);
            var actual = ShortcutCreator.Instance.Read(shortcut_path);
            return
                actual.TargetPath == expected.TargetPath &&
                actual.Arguments == expected.Arguments &&
                actual.IconLocation == expected.IconLocation &&
                actual.Description == expected.Description;
        }

        private void CreateShortcut(App app)
        {
            var location = Path.Combine(this.getStartMenuFolder(), $"{app.Name}.lnk");
            var shortcut = BuildShortcut(app);
            ShortcutCreator.Instance.Create(location, shortcut);
        }

        private Shortcut BuildShortcut(App app)
        {
            var shortcut = new Shortcut();
            if (app.CopyLocal)
                shortcut.TargetPath = Path.Combine(this.getInstallFolder(), app.Folder, app.Exe);
            else
                shortcut.TargetPath = Path.Combine(this.Config.BinarySource, app.Folder, app.Exe);
            shortcut.Arguments = app.Arguments ?? "";

            string icon_file = "";
            if (app.IconFile != null)
                icon_file = Path.Combine(this.getLocalIconFolder(), app.IconFile);
            var icon_index = app.IconIndex ?? 0;
            shortcut.IconLocation = $"{icon_file},{icon_index}";
            shortcut.Description = app.Name;

            return shortcut;
        }

        public string getInstallFolder()
        {
            return EnvironmentVariableSubFolder("LOCALAPPDATA", this.Config.LocalAppDataFolder, "Apps");
        }

        public string getSettingsFolder()
        {
            return EnvironmentVariableSubFolder("LOCALAPPDATA", this.Config.LocalAppDataFolder, "settings");
        }

        public void InstallAllApps()
        {
            var expected_apps = this.Config.Apps
                .Where(x => x.CopyLocal)
                .ToDictionary(x => x.Folder);

            var install_folder = this.getInstallFolder();
            if (Directory.Exists(install_folder))
            {
                var bin_folders = Directory.EnumerateDirectories(install_folder).ToList();
                foreach (var bin_folder in bin_folders)
                {
                    var folder = FileUtils.GetLeafFolder(bin_folder);
                    if (!expected_apps.ContainsKey(folder))
                        Directory.Delete(bin_folder);
                }
            }
            foreach (var expected_app in expected_apps.Values)
            {
                var folder = expected_app.Folder;
                var source_folder = Path.Combine(this.Config.BinarySource, folder);
                var dest_folder = Path.Combine(install_folder, folder);
                if (!Directory.Exists(dest_folder))
                    FileUtils.CopyFolderRecursive(source_folder, dest_folder);
            }
        }


        public void Remove()
        {
            if (Directory.Exists(this.getInstallFolder()))
                Directory.Delete(this.getInstallFolder(), true);
            if (Directory.Exists(this.getLocalIconFolder()))
                Directory.Delete(this.getLocalIconFolder(), true);
            if (Directory.Exists(this.getStartMenuFolder()))
                Directory.Delete(this.getStartMenuFolder(), true);
        }

        public void Install()
        {
            this.UpdateIcons();
            this.InstallAllApps();
            this.UpdateShortcuts();
        }

        public bool UpdateAvailable(string folder_name, string exe_name)
        {
            var binary_source = Path.Combine(this.Config.BinarySource, folder_name, exe_name);
            var binary_dest = Path.Combine(this.getInstallFolder(), folder_name, exe_name);

            var source_timestamp = File.GetLastWriteTimeUtc(binary_source);
            var dest_timestamp = File.GetLastWriteTimeUtc(binary_dest);

            return source_timestamp != dest_timestamp;
        }

        public void UpdateSingle(string folder_name, string exe_name)
        {
            var source_folder = Path.Combine(this.Config.BinarySource, folder_name);
            var dest_folder = Path.Combine(this.getInstallFolder(), folder_name);

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
            try
            {
                var p = Process.GetProcessById(pid);
                p.WaitForExit();
            } 
            catch(ArgumentException)
            {
                // this is what happens if there's no 
                // running process with the requested pid
            }
        }

        private string getLoopBlocker(string folder)
        {
            return Path.Combine(this.getInstallFolder(), folder, "loopblocker.txt");
        }

        public void CheckForUpdateAndRestartIfNecessary()
        {
            var entry_assembly = Assembly.GetEntryAssembly();
            var folder = FileUtils.GetLeafFolder(Path.GetDirectoryName(entry_assembly.Location));
            var exe = Path.GetFileName(entry_assembly.Location);

            if (entry_assembly.Location.StartsWith(this.getInstallFolder()))
            {
                this.CheckForUpdateAndClose(folder, exe);
            }
        }

        public bool DeployedVersionIsRunning()
        {
            var entry_assembly = Assembly.GetEntryAssembly();
            var install_folder = this.getInstallFolder();
            return entry_assembly.Location.StartsWith(install_folder);
        }

        public void CheckForUpdateAndClose(string folder, string exe)
        {
            var loop_blocker = this.getLoopBlocker(folder);
            if (UpdateAvailable(folder, exe))
            {
                if (File.Exists(loop_blocker))
                    throw new Exception("Found the loop blocker file.");
                else
                    File.WriteAllText(loop_blocker, "");

                var installer_path = Path.Combine(this.Config.BinarySource, "Installer", "Installer.exe");
                var arguments = $"UpdateAndStart {folder} {exe} {Process.GetCurrentProcess().Id}";
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(installer_path, arguments);
                p.Start();
                Environment.Exit(0);
            }
            else
            {
                if (File.Exists(loop_blocker))
                    File.Delete(loop_blocker);
            }
        }

        public void Launch(string folder_name, string exe_name)
        {
            var p = new Process();
            var app_path = Path.Combine(this.getInstallFolder(), folder_name, exe_name);
            p.StartInfo = new ProcessStartInfo(app_path);
            p.Start();
        }
    }
}