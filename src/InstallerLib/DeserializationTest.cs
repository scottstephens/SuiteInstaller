using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SuiteInstaller.InstallerLib
{
    public class DeserializationTest
    {
        public static void Run()
        {
            var config_text = File.ReadAllText(@"\\example.com\ProductionAppData\InHouseAppLaunchers\Apps.json");
            var escaped_config = config_text.Replace(@"\",@"\\");
            var settings = new JsonSerializerSettings();

            var config = JsonConvert.DeserializeObject<Config>(escaped_config);
        }
    }
}
