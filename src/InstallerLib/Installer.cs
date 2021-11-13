using log4net;
using Newtonsoft.Json;
using SuiteInstaller.InstallerLib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SuiteInstaller.InstallerLib
{
    public class Installer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Installer));

        private string ConfigFilePath;

        private Config _config;

        private Config Config
        {
            get
            {
                if (_config == null)
                {
                    this.FastNetworkCheck();
                    _config = ParseConfig(this.ConfigFilePath);
                }
                return _config;
            }
        }

        public Installer(string config_file)
        {
            this.ConfigFilePath = config_file;
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

        internal void InitializeLog4Net()
        {
            GlobalContext.Properties["LogFolder"] = this.getLogFolder();
            var log_config_path = Path.Combine(this.Config.BinarySource, "Installer", "log4net.xml");
            var log_repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(log_repository, new FileInfo(log_config_path));
        }

        public void FastNetworkCheck()
        {
            var check_task = Task.Run(this.CheckConfigFile);
            if (!check_task.Wait(TimeSpan.FromSeconds(5.0)))
            {
                ThreadPool.QueueUserWorkItem(x => WaitAndSwallow((Task)x), check_task);
                throw new SourceNotFoundException(this.ConfigFilePath);
            }
        }

        private void WaitAndSwallow(Task t)
        {
            try
            {
                t.Wait();
            }
            catch (Exception)
            {

            }
        }

        private void CheckConfigFile()
        {
            if (!File.Exists(this.ConfigFilePath))
                throw new SourceNotFoundException(this.ConfigFilePath);
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
            log.Debug("UpdateShortcuts()");
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

        public string getLogFolder()
        {
            return EnvironmentVariableSubFolder("LOCALAPPDATA", this.Config.LocalAppDataFolder, "Logs");
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
                        Directory.Delete(bin_folder, true);
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
            log.Info("Remove()");
            if (Directory.Exists(this.getInstallFolder()))
                Directory.Delete(this.getInstallFolder(), true);
            if (Directory.Exists(this.getLocalIconFolder()))
                Directory.Delete(this.getLocalIconFolder(), true);
            if (Directory.Exists(this.getStartMenuFolder()))
                Directory.Delete(this.getStartMenuFolder(), true);
        }

        public void Install()
        {
            log.Info("Install");
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
            log.InfoFormat("UpdateAndStart({0})", new StringArrayFormatter(args));
            var folder = args[1];
            var exe = args[2];
            if (args.Length > 3)
            {
                var pid = Int32.Parse(args[3]);
                this.WaitForProcessToClose(pid);
            }

            while (this.IsRunningProcess(folder, exe))
            {
                Console.WriteLine();
                Console.WriteLine("The program being updated is currently running.");
                Console.WriteLine("Close the program and then press enter.");
                Console.WriteLine("If you wish to keep the other program running, you may type cancel and then press enter;");
                Console.WriteLine("this will stop the upgrade process.");
                var input = Console.ReadLine();

                if (input.ToLowerInvariant() == "cancel")
                {
                    File.Delete(this.getLoopBlocker(folder));
                    return;
                }
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

        public bool IsRunningProcess(string folder, string exe)
        {
            var target_exe = Path.Combine(this.getInstallFolder(), folder, exe);
            var candidates = Process.GetProcesses();
            var all = new List<string>();
            foreach (var candidate in candidates)
            {
                bool has_exited;
                ProcessModule main_module;
                try
                {
                    has_exited = candidate.HasExited;
                }
                catch
                {
                    continue;
                }
                try
                {
                    main_module = candidate.MainModule;
                }
                catch
                {
                    continue;
                }

                all.Add(main_module.FileName);

                if (!has_exited && main_module.FileName == target_exe)
                {
                    return true;
                }
            }
            return false;
        }

        private string getLoopBlocker(string folder)
        {
            return Path.Combine(this.getInstallFolder(), folder, "loopblocker.txt");
        }

        public void CheckForUpdateAndRestartIfNecessary()
        {
            var info = this.getCurrentProcessInfo();

            if (info.IsDeployed)
            {
                this.CheckForUpdateAndClose(info.Folder, info.Exe);
            }
        }

        public class CurrentProcessInfo
        {
            public string PenultimateFolderPath;
            public string Folder;
            public string Exe;
            public bool IsDeployed;
        }

        public CurrentProcessInfo getCurrentProcessInfo()
        {
            var exe_path = Process.GetCurrentProcess().MainModule.FileName;
            var exe = Path.GetFileName(exe_path);

            var folder_path = Path.GetDirectoryName(exe_path);

            var pen_folder_path = Path.GetDirectoryName(folder_path);
            var folder = FileUtils.GetLeafFolder(folder_path);

            var install_folder = this.getInstallFolder();

            return new CurrentProcessInfo()
            {
                Exe = exe,
                Folder = folder,
                PenultimateFolderPath = pen_folder_path,
                IsDeployed = pen_folder_path.Equals(this.getInstallFolder()),
            };
        }

        public bool DeployedVersionIsRunning()
        {
            var info = this.getCurrentProcessInfo();
            return info.IsDeployed;
        }

        public void CheckForUpdateAndClose(string folder, string exe)
        {
            log.InfoFormat("CheckForUpdateAndClose({0},{1})", folder, exe);
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
            log.InfoFormat("Launch({0},{1})", folder_name, exe_name);
            var p = new Process();
            var app_path = Path.Combine(this.getInstallFolder(), folder_name, exe_name);
            p.StartInfo = new ProcessStartInfo(app_path);
            p.Start();
        }
    }
}
