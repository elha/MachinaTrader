using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MachinaTrader.Globals.Helpers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace MachinaTrader.Globals
{
    public class GlobalSettings
    {

        public void LogConfiguration()
        {
            //Create Logging Config if not exist
            if (!File.Exists(Global.DataPath + "/Logging.json"))
            {
                JObject loggingConfig = new JObject();
                loggingConfig["Logging"] = new JObject();
                loggingConfig["Logging"]["IncludeScopes"] = false;
                loggingConfig["Logging"]["Debug"] = new JObject();
                loggingConfig["Logging"]["Debug"]["LogLevel"] = new JObject();
                loggingConfig["Logging"]["Debug"]["LogLevel"]["Default"] = "Information";
                loggingConfig["Logging"]["Debug"]["LogLevel"]["Microsoft"] = "Error";
                loggingConfig["Logging"]["Console"] = new JObject();
                loggingConfig["Logging"]["Console"]["LogLevel"] = new JObject();
                loggingConfig["Logging"]["Console"]["LogLevel"]["Default"] = "Information";
                loggingConfig["Logging"]["Console"]["LogLevel"]["Microsoft"] = "Error";

                loggingConfig["Serilog"] = new JObject();
                loggingConfig["Serilog"]["Using"] = new JArray();
                ((JArray)loggingConfig["Serilog"]["Using"]).Add("Serilog.Sinks.RollingFile");
                loggingConfig["Serilog"]["MinimumLevel"] = new JObject();
                loggingConfig["Serilog"]["MinimumLevel"]["Default"] = "Information"; //Debug ?!
                loggingConfig["Serilog"]["MinimumLevel"]["Override"] = new JObject();
                loggingConfig["Serilog"]["MinimumLevel"]["Override"]["Microsoft"] = "Error"; //Dont log ASP msg -> Set to Information if needed
                loggingConfig["Serilog"]["MinimumLevel"]["Override"]["System"] = "Warning";
                loggingConfig["Serilog"]["WriteTo"] = new JArray();

                JObject writeToObject = new JObject();
                writeToObject["Name"] = "Async";
                writeToObject["Args"] = new JObject();
                writeToObject["Args"]["configure"] = new JArray();

                JObject rollingFile = new JObject();
                rollingFile["Name"] = "RollingFile";
                rollingFile["Args"] = new JObject();
                rollingFile["Args"]["configure"] = new JArray();

                JObject rollingFileArgs = new JObject();
                rollingFileArgs["Name"] = "RollingFile";
                rollingFileArgs["Args"] = new JObject();
                rollingFileArgs["Args"]["pathFormat"] = Global.DataPath + "/Logs/MachinaTrader-{Date}.log";

                ((JArray)rollingFile["Args"]["configure"]).Add(rollingFileArgs);
                ((JArray)writeToObject["Args"]["configure"]).Add(rollingFile);
                ((JArray)loggingConfig["Serilog"]["WriteTo"]).Add(writeToObject);

                loggingConfig["Serilog"]["Enrich"] = new JArray();
                ((JArray)loggingConfig["Serilog"]["Enrich"]).Add("FromLogContext");

                string jsonToFile = JsonConvert.SerializeObject(loggingConfig, Formatting.Indented);
                File.WriteAllText(Global.DataPath + "/Logging.json", jsonToFile);
            }

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Global.DataPath + "/Logging.json")
                .Build();

            Global.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            Global.Logger.Information("Starting");
            Global.Logger.Information("BasePath: " + Global.AppPath);
            Global.Logger.Information("DataFolder: " + Global.DataPath.Replace("\\", "/"));
        }

        public void Folders()
        {
            Global.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)?.Replace("\\", "/");
            while (true)
            {
                //We are in development mode -> Loop though parent folders to find wwwroot Folder
                if (Directory.Exists(Global.AppPath + "/wwwroot"))
                {
                    break;
                }
                Global.AppPath = Directory.GetParent(Global.AppPath).FullName.Replace("\\", "/");
            }

            Global.AppParentPath = Directory.GetParent(Global.AppPath).FullName.Replace("\\", "/");

            //Check if we are in portable environment -> In this case Data Folder is in parent folder to prevent update errors
            if (Directory.Exists(Directory.GetParent(Global.AppPath).FullName + "/Data"))
            {
                Global.DataPath = Directory.GetParent(Global.AppPath).FullName.Replace("\\", "/") + "/Data";
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
