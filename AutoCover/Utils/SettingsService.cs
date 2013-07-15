using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.IO;
using System.Xml.Serialization;

namespace AutoCover
{
    public class SettingsService
    {
        private static AutoCoverSettings _currentSettings;

        public static void LoadSettingsForSolution(Solution solution)
        {
            var settingsPath = GetSettingsPath(solution);
            if (!File.Exists(settingsPath))
            {
                _currentSettings = new AutoCoverSettings();

            }
            else
            {
                var serializer = new XmlSerializer(typeof(AutoCoverSettings));
                using (var txtW = new StreamReader(settingsPath))
                {
                    _currentSettings = (AutoCoverSettings)serializer.Deserialize(txtW);
                }
            }
        }

        public static AutoCoverSettings Settings
        {
            get { return _currentSettings ?? new AutoCoverSettings(); }
        }

        private static string GetSettingsPath(Solution solution)
        {
            var solutionPath = Path.GetDirectoryName(solution.FullName);
            var settingsPath = Path.Combine(solutionPath, "_AutoCover", "autocover.settings");
            return settingsPath;
        }

        public static void UnloadSettings(Solution solution)
        {
            if (_currentSettings != null)
            {
                var settingsPath = GetSettingsPath(solution);
                var serializer = new XmlSerializer(typeof(AutoCoverSettings));
                using (var txtW = new StreamWriter(settingsPath))
                {
                    serializer.Serialize(txtW, _currentSettings);
                }
                _currentSettings = null;
            }
        }

    }

    public class AutoCoverSettings
    {
        public bool EnableAutoCover { get; set; }

        public bool DisableRowHighlighting { get; set; }
    }
}
