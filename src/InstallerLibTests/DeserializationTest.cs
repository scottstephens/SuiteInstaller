using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SuiteInstaller.InstallerLib;

[TestFixture]
public class DeserializationTest
{
    [Test]
    public static void Run()
    {
        var filePath = Path.Combine("data", "ExampleConfig.yaml");
        var config = Installer.ParseConfigYaml(filePath);
    }
}
