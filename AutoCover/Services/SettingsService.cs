// Copyright (c) 2013
// Simone Grignola [http://www.grignola.ch]
//
// This file is part of AutoCover.
//
// AutoCover is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
            var settingsFileName = Path.GetFileNameWithoutExtension(solution.FullName) + ".acsettings";
            return Path.Combine(solutionPath, "_AutoCover", settingsFileName);
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
