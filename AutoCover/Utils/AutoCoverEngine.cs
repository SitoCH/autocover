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
                            return new List<UnitTest>();
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building));
                        // Build the tests projects
                        var testProjects = currentTests.Select(x => x.ProjectName).Distinct().ToList();
                        var testAssemblies = new List<TestAssembly>();
                        foreach (Project project in solution.Projects)
                        {
                            if (testProjects.Contains(project.Name))
                            {
                                Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building, project.Name));
                                solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);
                                if (solution.SolutionBuild.LastBuildInfo == 0)
                                {
                                    var projectOutputFile = Instrument(project);
                                    var ta = new TestAssembly { Name = project.Name, DllPath = projectOutputFile };
                                    testAssemblies.Add(ta);
                                    var coverageFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "coverage.xml");
                                    File.Copy(coverageFile, coverageFile + ".clean", true);
                                }
                            }
                        }
                        var tests = Utils.FilterTests(document, _testResults, _coverageResults, tmi.GetTests().ToList());
                        if (testAssemblies.Count == 0 || currentTests.Count == 0)
                            return new List<UnitTest>();
                        testAssemblies.ForEach(ta => ta.Tests = tests.Where(x => x.ProjectData.ProjectName == ta.Name).ToList());

                        var msTestPathExe = Utils.GetMSTestPath();
                        var processRunner = new ProcessRunner(msTestPathExe, Path.GetDirectoryName(msTestPathExe));
                        foreach (var testAssembly in testAssemblies)
                        {
                            Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing, testAssembly.Name));
                            var projectOutputFile = testAssembly.DllPath;
                            var testResultsFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "test.trx");
                            var coverageFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "coverage.xml");
                            File.Copy(coverageFile + ".clean", coverageFile, true);
                            MSTestRunner.Run(processRunner, projectOutputFile, testResultsFile, testSettingsPath, testAssembly.Tests, _testResults);
                            //ParseCoverageResults(coverageFile, _coverageResult, test.HumanReadableId);
                            File.Delete(testResultsFile);
                        }
                        return _testResults.GetTestResults().Values.ToList();
                    }
                }).ContinueWith(ct =>
                    {
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Idle));
                        Messenger.Default.Send(new TestsResultsMessage(ct.Result));
                    });

        }

        private static string Instrument(Project project)
        {
            var basePath = project.Properties.Item("FullPath").Value.ToString();
            var config = project.ConfigurationManager.ActiveConfiguration;
            var outputPath = config.Properties.Item("OutputPath").Value.ToString();
            var dllsPath = Path.Combine(basePath, outputPath);
            var newPath = Path.Combine(dllsPath, "..\\_AutoCover");
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

        /*private static void ParseCoverageResults(string coverageFile, CoverageResults coverageResult, string test)
        {
            using (var coverageStream = new FileStream(coverageFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                var xDoc = XDocument.Load(new XmlTextReader(coverageStream));
                foreach (var module in xDoc.Descendants("module"))
                {
                    foreach (var pt in module.Descendants("seqpnt"))
                    {
                        var visitCount = int.Parse(pt.Attribute("visitcount").Value);
                        if (visitCount > 0)
                        {
                            var document = pt.Attribute("document").Value;
                            var cb = new CodeBlock
                                {
                                    VisitCount = visitCount,
                                    Line = int.Parse(pt.Attribute("line").Value),
                                    Column = int.Parse(pt.Attribute("column").Value),
                                    EndLine = int.Parse(pt.Attribute("endline").Value),
                                    EndColumn = int.Parse(pt.Attribute("endcolumn").Value)
                                };
                            coverageResult.ProcessCodeBlock(test, document, cb);
                        }
                    }
                }
            }
        } */


        private static IEnumerable<string> GetAssemblies(string fullPath)
        {
            return Directory.GetFiles(fullPath).Where(file => (Path.GetExtension(file) == ".dll" || Path.GetExtension(file) == ".exe") && File.Exists(Path.ChangeExtension(file, "pdb")));
        }
    }
}
