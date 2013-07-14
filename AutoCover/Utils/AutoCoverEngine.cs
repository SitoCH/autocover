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
        public static DateTime LastCheck { get; private set; }

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

                                solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);
                                if (solution.SolutionBuild.LastBuildInfo == 0)
                                {
                                    var projectOutputFile = CodeCoverageService.Instrument(solution, project);
                                    var ta = new TestAssembly { Name = project.Name, DllPath = projectOutputFile };
                                    testAssemblies.Add(ta);
                                }
                            }
                        }
                        // Run all the impacted tests
                        var tests = Utils.FilterTests(document, _testResults, _coverageResults, tmi.GetTests().ToList());
                        if (testAssemblies.Count == 0 || tests.Count == 0)
                            return _testResults.GetTestResults().Values.ToList();
                        testAssemblies.ForEach(ta => ta.Tests = tests.Where(x => x.ProjectData.ProjectName == ta.Name).ToList());
                        // Run the tests and parse the results
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
                            CodeCoverageService.ParseCoverageResults(coverageFile, tests, _coverageResults);
                        }
                        return _testResults.GetTestResults().Values.ToList();
                    }
                }).ContinueWith(ct =>
                {
                    LastCheck = DateTime.Now;
                    Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Idle));
                    Messenger.Default.Send(new TestsResultsMessage(ct.Result));
                    Messenger.Default.Send(new RefreshTaggerMessage());
                });
        }

        public static CodeCoverageResult GetLineResult(string document, int line)
        {
            var testIds = _coverageResults.GetTestsFor(document, line);
            if (testIds == null || !testIds.Any())
                return CodeCoverageResult.NotCovered;
            if (testIds.Select(x => _testResults.GetTestResults()[x]).Any(x => x.Result == UnitTestResult.Failed))
                return CodeCoverageResult.Failed;
            return CodeCoverageResult.Passed;
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _coverageResults.Reset();
                _testResults.Reset();
            }
        }
    }
}
