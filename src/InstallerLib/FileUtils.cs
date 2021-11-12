using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SuiteInstaller.InstallerLib.Utilities
{
    public class FileUtils
    {
        public static void CopyFolderRecursive(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                if (!File.Exists(dest))
                    File.Copy(file, dest);
                else
                {
                    var source_ts = File.GetLastWriteTimeUtc(file);
                    var dest_ts = File.GetLastWriteTimeUtc(dest);
                    if (dest_ts != source_ts)
                        File.Copy(file, dest, overwrite: true);
                }
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolderRecursive(folder, dest);
            }
        }

        public static void SyncFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                var dest = Path.Combine(destFolder, name);
                if (!File.Exists(dest))
                    File.Copy(file, dest);
                else
                {
                    var src_ts = File.GetLastWriteTime(file);
                    var dest_ts = File.GetLastWriteTime(dest);
                    if (dest_ts < src_ts)
                        File.Copy(file, dest, overwrite: true);
                }
            }
        }

        public static string GetLeafFolder(string folder_path)
        {
            if (File.Exists(folder_path))
                throw new ArgumentException($"Input path is a file: {folder_path}");
            if (!Directory.Exists(folder_path))
                throw new ArgumentException($"Input path doesn't exist: {folder_path}");
            var result1 = Path.GetFileName(folder_path);
            if (result1 != "")
                return result1;
            else
                return Path.GetFileName(Path.GetDirectoryName(folder_path));
        }
    }
}
