using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace CustomISLC
{
    public class AppConfig
    {
        public string StandbyListValue { get; set; }
        public string FreeMemoryValue { get; set; }
        public bool StartMinimized { get; set; }
        public bool AutoStartMonitoring { get; set; }
        public bool MinimizeOnClose { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool CustomTimer { get; set; }
        public double WantedResolution { get; set; }
        public string PollingRateSTR { get; set; }
        public List<string> ExclusionList { get; set; }
        public bool DarkMode { get; set; }
        public bool HotkeyEnabled { get; set; }
        public int HotkeyModifiers { get; set; }
        public int HotkeyVKey { get; set; }
        public bool PurgeWorkingSetOnAuto { get; set; }
        public bool TriggerConditionOr { get; set; }

        // Novos Módulos de Otimização de Entrega de Imagem (Frametime / GPU / CPU)
        public bool SmartLowPriorityPurge { get; set; }
        public bool FlushModifiedBeforePurge { get; set; }
        public bool AutoBoostForeground { get; set; }
        public bool MmcssGpuOptimization { get; set; }
        public bool UltimatePowerPlan { get; set; }

        public AppConfig()
        {
            StandbyListValue = "1024";
            FreeMemoryValue = "8000";
            StartMinimized = false;
            AutoStartMonitoring = true;
            MinimizeOnClose = true;
            AlwaysOnTop = false;
            CustomTimer = true;
            WantedResolution = 0.5;
            PollingRateSTR = "1000";
            ExclusionList = new List<string>();
            DarkMode = true;
            HotkeyEnabled = false;
            HotkeyModifiers = 3; // Ctrl + Alt
            HotkeyVKey = 80;     // P
            PurgeWorkingSetOnAuto = false;
            TriggerConditionOr = false;

            SmartLowPriorityPurge = true;
            FlushModifiedBeforePurge = false;
            AutoBoostForeground = true;
            MmcssGpuOptimization = true;
            UltimatePowerPlan = false;
        }

        private static string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"); }
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig loaded = serializer.Deserialize<AppConfig>(json);
                    if (loaded != null)
                    {
                        if (loaded.ExclusionList == null)
                            loaded.ExclusionList = new List<string>();
                        return loaded;
                    }
                }
            }
            catch
            {
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendFormat("  \"StandbyListValue\": \"{0}\",\r\n", Escape(StandbyListValue));
                sb.AppendFormat("  \"FreeMemoryValue\": \"{0}\",\r\n", Escape(FreeMemoryValue));
                sb.AppendFormat("  \"StartMinimized\": {0},\r\n", StartMinimized ? "true" : "false");
                sb.AppendFormat("  \"AutoStartMonitoring\": {0},\r\n", AutoStartMonitoring ? "true" : "false");
                sb.AppendFormat("  \"MinimizeOnClose\": {0},\r\n", MinimizeOnClose ? "true" : "false");
                sb.AppendFormat("  \"AlwaysOnTop\": {0},\r\n", AlwaysOnTop ? "true" : "false");
                sb.AppendFormat("  \"CustomTimer\": {0},\r\n", CustomTimer ? "true" : "false");
                sb.AppendFormat(CultureInfo.InvariantCulture, "  \"WantedResolution\": {0:0.##},\r\n", WantedResolution);
                sb.AppendFormat("  \"PollingRateSTR\": \"{0}\",\r\n", Escape(PollingRateSTR));

                sb.Append("  \"ExclusionList\": [");
                if (ExclusionList != null && ExclusionList.Count > 0)
                {
                    sb.AppendLine();
                    for (int i = 0; i < ExclusionList.Count; i++)
                    {
                        string comma = (i < ExclusionList.Count - 1) ? "," : "";
                        sb.AppendFormat("    \"{0}\"{1}\r\n", Escape(ExclusionList[i]), comma);
                    }
                    sb.Append("  ],\r\n");
                }
                else
                {
                    sb.AppendLine("],");
                }

                sb.AppendFormat("  \"DarkMode\": {0},\r\n", DarkMode ? "true" : "false");
                sb.AppendFormat("  \"HotkeyEnabled\": {0},\r\n", HotkeyEnabled ? "true" : "false");
                sb.AppendFormat("  \"HotkeyModifiers\": {0},\r\n", HotkeyModifiers);
                sb.AppendFormat("  \"HotkeyVKey\": {0},\r\n", HotkeyVKey);
                sb.AppendFormat("  \"PurgeWorkingSetOnAuto\": {0},\r\n", PurgeWorkingSetOnAuto ? "true" : "false");
                sb.AppendFormat("  \"TriggerConditionOr\": {0},\r\n", TriggerConditionOr ? "true" : "false");
                sb.AppendFormat("  \"SmartLowPriorityPurge\": {0},\r\n", SmartLowPriorityPurge ? "true" : "false");
                sb.AppendFormat("  \"FlushModifiedBeforePurge\": {0},\r\n", FlushModifiedBeforePurge ? "true" : "false");
                sb.AppendFormat("  \"AutoBoostForeground\": {0},\r\n", AutoBoostForeground ? "true" : "false");
                sb.AppendFormat("  \"MmcssGpuOptimization\": {0},\r\n", MmcssGpuOptimization ? "true" : "false");
                sb.AppendFormat("  \"UltimatePowerPlan\": {0}\r\n", UltimatePowerPlan ? "true" : "false");
                sb.Append("}");

                File.WriteAllText(ConfigPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
