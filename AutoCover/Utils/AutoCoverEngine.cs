using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using EnvDTE;
using GalaSoft.MvvmLight.Messaging;
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
                        var settings = SettingsService.Settings;
                        if (!settings.EnableAutoCover)
                            return new List<UnitTest>();

                        var currentTests = tmi.GetTests().ToList();
                        if (currentTests.Count == 0)
                            return _testResults.GetTestResults().Values.ToList();
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building));
                        // Build the tests projects
                        var testAssemblies = new List<TestAssembly>();
                        foreach (Project project in solution.Projects)
                        {
                            var ids = project.GetProjectTypeGuids();
                            if (ids.Contains("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}"))
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
                        var tests = FilterTests(document, _testResults, _coverageResults, tmi.GetTests().ToList());
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

        private static List<ITestElement> FilterTests(Document document, TestResults testsResults, CoverageResults coverageResults, List<ITestElement> suggestedTests)
        {
            if (testsResults.GetTestResults().Count == 0)
                return suggestedTests;

            testsResults.RemoveDeletedTests(suggestedTests);

            var impactedTests = coverageResults.GetImpactedTests(document.FullName);
            var oldTests = new HashSet<Guid>(testsResults.GetTestResults().Keys);

            return suggestedTests.Where(x => impactedTests.Contains(x.Id.Id) || !oldTests.Contains(x.Id.Id)).ToList();
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
