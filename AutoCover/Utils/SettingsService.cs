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
        private static Tuple<string, AutoCoverSettings> _currentSettings;

        public static AutoCoverSettings GetSettingsForSolution(Solution solution)
        {
            if (_currentSettings != null && _currentSettings.Item1 == solution.FullName)
                return _currentSettings.Item2;

            var settingsPath = GetSettingsPath(solution);
            if (!File.Exists(settingsPath))
            {
                _currentSettings = new Tuple<string, AutoCoverSettings>(solution.FullName, new AutoCoverSettings());

            }
            else
            {
                var serializer = new XmlSerializer(typeof(AutoCoverSettings));
                using (var txtW = new StreamReader(settingsPath))
                {
                    _currentSettings = new Tuple<string, AutoCoverSettings>(solution.FullName, (AutoCoverSettings)serializer.Deserialize(txtW));
                }
            }
            return _currentSettings.Item2;
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
                    serializer.Serialize(txtW, _currentSettings.Item2);
                }
                _currentSettings = null;
            }
        }

    }

    public class AutoCoverSettings
    {
        public bool EnableAutoCover { get; set; }
    }
}
