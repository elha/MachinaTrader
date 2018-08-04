using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MachinaTrader.Globals.Helpers;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Globals
{
    public class GlobalSettings
    {
        public void Folders()
        {
            Global.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)?.Replace("\\", "/");
            while (true)
            {
                //We are in development mode -> Loop though parent folders to find wwwroot Folder
                if (Directory.Exists(Global.AppPath + "/wwwroot"))
                {
                    Global.Logger.Information("Found BasePath: " + Global.AppPath);
                    break;
                }
                Global.Logger.Information("Try to get wwwroot in BasePath: " + Global.AppPath);
                Global.AppPath = Directory.GetParent(Global.AppPath).FullName.Replace("\\", "/");
            }

            Global.AppParentPath = Directory.GetParent(Global.AppPath).FullName.Replace("\\", "/");

            //Check if we are in portable environment -> In this case Data Folder is in parent folder to prevent update errors
            if (Directory.Exists(Directory.GetParent(Global.AppPath).FullName + "/Data"))
            {
                Global.DataPath = Directory.GetParent(Global.AppPath).FullName.Replace("\\", "/") + "/Data";
                Global.Logger.Information("DataFolder Exists - Set Data Folder to " + Global.DataPath.Replace("\\", "/"));
            } else
            {
                Global.DataPath = (Global.AppPath + "/Data").Replace("\\", "/");
            }

            //Check Data Folder
            if (!Directory.Exists(Global.DataPath))
            {
                Directory.CreateDirectory(Global.DataPath);
            }
        }


        public void DefaultCoreSettings()
        {
            Global.CoreConfig["coreConfig"] = new JObject
            {
                ["enableDebug"] = false,
                ["enableDevelopment"] = false
            };


            Global.CoreConfig["coreConfig"] = new JObject
            {
                ["webPort"] = 8888,
                ["webLocalHostOnly"] = false,
                ["webDefaultUsername"] = "admin",
                ["webDefaultUserEmail"] = "admin@localhost",
                ["webDefaultPassword"] = "admin"
            };
        }

        public void CommonFiles()
        {
            Global.CoreConfig = MergeObjects.MergeCsDictionaryAndSave(Global.CoreConfig, Global.DataPath + "/CoreConfig.json");
        }

        public void DefaultCoreRuntimeSettings()
        {
            Global.CoreRuntime["Plugins"] = new JObject();

            foreach (var file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFileSystemInfos("MachinaTrader.Plugin.*.dll", SearchOption.TopDirectoryOnly))
            {
                string pluginName = (file.Name).Replace(".dll", "");

                Global.CoreRuntime["Plugins"][pluginName] = new JObject
                {
                    ["Enabled"] = true,
                    ["WwwRoot"] = null,
                    ["WwwRootDataFolder"] = null
                };

                if (File.Exists(Global.DataPath + "/" + pluginName + "/Config.json"))
                {
                    JObject pluginConfig =
                        JObject.Parse(File.ReadAllText(Global.DataPath + "/" + pluginName + "/Config.json"));
                    try
                    {
                        Global.CoreRuntime["Plugins"][pluginName]["Enabled"] = (bool) pluginConfig["Plugin"]["Enabled"];
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (Directory.Exists(Global.AppPath + "/Plugins/" + pluginName + "/wwwroot"))
                {
                    Global.CoreRuntime["Plugins"][pluginName]["WwwRoot"] = Global.AppPath + "/Plugins/" + pluginName + "/wwwroot";
                }
                else if (Directory.Exists((string)Global.AppParentPath + "/" + pluginName + "/wwwroot"))
                {
                    Global.CoreRuntime["Plugins"][pluginName]["WwwRoot"] = (string)Global.AppParentPath + "/" + pluginName + "/wwwroot";
                    Global.Logger.Information(pluginName + " Base Folder not found - Trying developent Path " + (string)Global.AppParentPath + "/" + pluginName + "/wwwroot");
                }

                //Check Data Folder
                if (Directory.Exists(Global.DataPath + "/" + pluginName + "/wwwroot"))
                {
                    Global.CoreRuntime["Plugins"][pluginName]["WwwRootDataFolder"] = Global.DataPath + "/" + pluginName + "/wwwroot";
                }
            }
        }
    }
}
