using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using EnvDTE;
using Coverage.Common;
using Coverage.Report;
using Coverage.Instrument;
using Coverage;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.VisualStudio.TestTools.Vsip;
using Microsoft.VisualStudio.TestTools.Common;

namespace AutoCover
{
    public static class AutoCoverEngine
    {
        private static readonly object _lock = new object();
        private static readonly CoverageResults _coverageResults = new CoverageResults();
        private static readonly TestResults _testResults = new TestResults();

        public static void CheckSolution(Solution solution, Document document, ITmi tmi, string testSettingsPath)
        {
            Task.Factory.StartNew(() =>
                {
                    lock (_lock)
                    {
                        var currentTests = tmi.GetTests().ToList();
                        if (currentTests.Count == 0)
                            return _testResults.GetTestResults().Values.ToList();
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building));
                        // Build the tests projects
                        var testProjects = currentTests.Select(x => x.ProjectName).Distinct().ToList();
                        var testAssemblies = new List<TestAssembly>();
                        foreach (Project project in solution.Projects)
                        {
                            if (testProjects.Contains(project.Name))
                            {
                                Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building, project.Name));
                                var runner = new ProcessRunner(Environment.ExpandEnvironmentVariables(@"%windir%\Microsoft.net\Framework\v4.0.30319\msbuild.exe"), Path.GetDirectoryName(solution.FullName));
                                var buildOutput = runner.Run(string.Format("\"{0}\"", project.FullName));
                                if (buildOutput.Item2 == 0)
                                {
                                    var projectOutputFile = Instrument(solution, project);
                                    var ta = new TestAssembly { Name = project.Name, DllPath = projectOutputFile };
                                    testAssemblies.Add(ta);
                                }
                            }
                        }
                        var tests = Utils.FilterTests(document, _testResults, _coverageResults, tmi.GetTests().ToList());
                        if (testAssemblies.Count == 0 || tests.Count == 0)
                            return _testResults.GetTestResults().Values.ToList();
                        testAssemblies.ForEach(ta => ta.Tests = tests.Where(x => x.ProjectData.ProjectName == ta.Name).ToList());

                        var msTestPathExe = Utils.GetMSTestPath();
                        var processRunner = new ProcessRunner(msTestPathExe, Path.GetDirectoryName(msTestPathExe));
                        foreach (var testAssembly in testAssemblies)
                        {
                            Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing, string.Format("{0} ({1} tests)", testAssembly.Name, testAssembly.Tests.Count)));
                            var projectOutputFile = testAssembly.DllPath;
                            var testResultsFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "test.trx");
                            MSTestRunner.Run(processRunner, projectOutputFile, testResultsFile, testSettingsPath, testAssembly.Tests, _testResults);
                            Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing, string.Format("{0} (parsing coverage results)", testAssembly.Name)));
                            var coverageFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "coverage.results.xml");
                            ParseCoverageResults(coverageFile, tests, _coverageResults);
                        }
                        return _testResults.GetTestResults().Values.ToList();
                    }
                }).ContinueWith(ct =>
                    {
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Idle));
                        Messenger.Default.Send(new TestsResultsMessage(ct.Result));
                        Messenger.Default.Send(new RefreshTaggerMessage());
                    });

        }

        public static bool IsLineCovered(string document, int line)
        {
            return _coverageResults.IsLineCovered(document, line);
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _coverageResults.Reset();
                _testResults.Reset();
            }
        }

        private static string Instrument(Solution solution, Project project)
        {
            var basePath = project.Properties.Item("FullPath").Value.ToString();
            var solutionPath = Path.GetDirectoryName(solution.FullName);
            var config = project.ConfigurationManager.ActiveConfiguration;
            var outputPath = config.Properties.Item("OutputPath").Value.ToString();
            var dllsPath = Path.Combine(basePath, outputPath);
            var newPath = Path.Combine(solutionPath, "_AutoCover", project.Name);
            try
            {
                if (Directory.Exists(newPath))
                    Directory.Delete(newPath, true);
            }
            catch { }
            Utils.Copy(dllsPath, newPath);
            Runner.Run(newPath, GetAssemblies(newPath));
            var fileName = project.Properties.Item("OutputFileName").Value.ToString();
            return Path.Combine(newPath, fileName);
        }

        private static void ParseCoverageResults(string coverageFile, List<ITestElement> tests, CoverageResults coverageResult)
        {
            var testsCache = tests.ToDictionary(k => k.HumanReadableId, e => e.Id.Id);

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
