using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Models
{
    public class RuntimeConfig
    {
        public string OS { get; set; } = GetOs();

        private static string GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Linux";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "OSX";
            }

            return "Unknown";
        }

        public string ComputerName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public JObject Plugins { get; set; }
        public List<string> SignalrClients { get; set; }
    }
}
