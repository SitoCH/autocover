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
using Microsoft.VisualStudio.TestTools.Common;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using Coverage;

namespace AutoCover
{
    public static class CodeCoverageService
    {
        public static string Instrument(Solution solution, Project project)
        {
            var basePath = project.Properties.Item("FullPath").Value.ToString();
            var solutionPath = Path.GetDirectoryName(solution.FullName);
            var config = project.ConfigurationManager.ActiveConfiguration;
            var outputPath = config.Properties.Item("OutputPath").Value.ToString();
            var dllsPath = Path.Combine(basePath, outputPath);
            var newPath = Path.Combine(solutionPath, "_AutoCover", project.Name);

            SmartDelete(newPath);
            var filesAlreadyInstrumented = new HashSet<string>();
            SmartCopy(dllsPath, newPath, filesAlreadyInstrumented);
            Runner.Run(newPath, GetAssemblies(newPath).Where(x => !filesAlreadyInstrumented.Contains(x)).ToList());
            var fileName = project.Properties.Item("OutputFileName").Value.ToString();
            return Path.Combine(newPath, fileName);
        }

        private static void SmartDelete(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (CanDeleteFile(file))
                    File.Delete(file);
            }

            foreach (string directory in Directory.GetDirectories(path))
                SmartDelete(directory);

            if (!Directory.GetDirectories(path).Any() && !Directory.GetFiles(path).Any())
                Directory.Delete(path);
        }

        private static bool CanDeleteFile(string file)
        {
            if (file.EndsWith("backup_dll"))
                return false;
            if (file.EndsWith("dll") && File.Exists(Path.ChangeExtension(file, "backup_dll")))
                return false;
            return true;
        }

        public static void SmartCopy(string sourceDir, string targetDir, ICollection<string> filesAlreadyInstrumented)
        {
            Directory.CreateDirectory(targetDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                var newPath = Path.Combine(targetDir, Path.GetFileName(file));
                var backupFilePath = Path.ChangeExtension(newPath, "backup_dll");
                if (File.Exists(backupFilePath) && File.Exists(newPath))
                {
                    if (FileUtils.FileEquals(file, backupFilePath))
                    {
                        filesAlreadyInstrumented.Add(newPath);
                        continue;
                    }
                }
                File.Copy(file, newPath, true);
            }
            foreach (string directory in Directory.GetDirectories(sourceDir))
                SmartCopy(directory, Path.Combine(targetDir, Path.GetFileName(directory)), filesAlreadyInstrumented);
        }

        public static void ParseCoverageResults(string coverageFile, List<ACUnitTest> tests, CoverageResults coverageResult)
        {
            var testsCache = tests.ToDictionary(k => k.HumanReadableId, e => e.Id);

            using (var coverageStream = new FileStream(coverageFile, FileMode.Open))
            {
                var xDoc = XDocument.Load(new XmlTextReader(coverageStream));
                foreach (var result in xDoc.Descendants("results"))
                {
                    foreach (var pt in result.Descendants("seqpnt"))
                    {
                        var document = pt.Attribute("document").Value;
                        var cb = new CodeBlock
                        {
                            Line = int.Parse(pt.Attribute("line").Value),
                            Column = int.Parse(pt.Attribute("column").Value),
                            EndLine = int.Parse(pt.Attribute("endline").Value),
                            EndColumn = int.Parse(pt.Attribute("endcolumn").Value)
                        };
                        foreach (var test in pt.Descendants())
                        {
                            var testName = test.Attribute("name").Value;
                            coverageResult.ProcessCodeBlock(testsCache[testName], document, cb);
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetAssemblies(string fullPath)
        {
            return Directory.GetFiles(fullPath).Where(file => (Path.GetExtension(file) == ".dll" || Path.GetExtension(file) == ".exe") && File.Exists(Path.ChangeExtension(file, "pdb")));
        }
    }
}
