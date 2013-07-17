using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using EnvDTE;
using GalaSoft.MvvmLight.Messaging;

namespace AutoCover
{
    public static class AutoCoverEngine
    {
        private static readonly BlockingCollection<AnalysisArgs> _queue = new BlockingCollection<AnalysisArgs>();
        private static readonly object _resetLock = new object();

        private static readonly CoverageResults _coverageResults = new CoverageResults();
        private static readonly TestResults _testResults = new TestResults();
        public static DateTime LastCheck { get; private set; }

        public static void AddDocument(Solution solution, Document document, string testSettingsPath)
        {
            if (SettingsService.Settings.EnableAutoCover)
                _queue.Add(new AnalysisArgs { Solution = solution, Document = document, TestSettingsPath = testSettingsPath });
        }

        public static void InitEngine()
        {
            Task.Factory.StartNew(Spool, TaskCreationOptions.LongRunning);
        }

        private static void Spool()
        {
            foreach (var args in _queue.GetConsumingEnumerable())
            {
                lock (_resetLock)
                {
                    ProcessDocument(args);
                }
            }
        }

        private static void ProcessDocument(AnalysisArgs args)
        {
            if (!SettingsService.Settings.EnableAutoCover)
            {
                Messenger.Default.Send(new TestsResultsMessage(new List<UnitTest>()));
                Messenger.Default.Send(new RefreshTaggerMessage());
                return;
            }

            var document = args.Document;
            var solution = args.Solution;
            try
            {
                Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building));
                // Build the tests projects
                var testAssemblies = new List<TestAssembly>();
                var suggestedTests = new List<UnitTest>();
                foreach (Project project in solution.Projects)
                {
                    var ids = project.GetProjectTypeGuids();
                    if (ids.Contains("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}"))
                    {
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building, project.Name));
                        var activeConfig = solution.Properties.Item("ActiveConfig").Value.ToString();
                        var runner = new ProcessRunner(Environment.ExpandEnvironmentVariables(Utils.GetDevEnvPath()), Path.GetDirectoryName(solution.FullName));
                        var buildOutput = runner.Run(string.Format("\"{0}\" /build \"{1}\"  /project \"{2}\"", solution.FullName, activeConfig, project.Name));
                        if (buildOutput.Item2 == 0)
                        {
                            Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Instrumenting, project.Name));
                            var projectOutputFile = CodeCoverageService.Instrument(solution, project);
                            var ta = new TestAssembly { Name = project.Name, DllPath = projectOutputFile };
                            suggestedTests.AddRange(MSTestRunner.GetTests(project.Name, projectOutputFile));
                            testAssemblies.Add(ta);
                        }
                    }
                }
                if (testAssemblies.Count == 0)
                    return;
                // Get all the impacted tests
                var tests = FilterTests(document, _testResults, _coverageResults, suggestedTests);
                if (tests.Count == 0)
                    return;
                // Reassign the tests to the right assembly
                testAssemblies.ForEach(ta => ta.Tests = tests.Where(x => x.ProjectName == ta.Name).ToList());
                // Run the tests and parse the results
                var msTestPathExe = Utils.GetMSTestPath();
                var processRunner = new ProcessRunner(msTestPathExe, Path.GetDirectoryName(msTestPathExe));
                foreach (var testAssembly in testAssemblies)
                {
                    Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing, string.Format("{0} ({1} tests)", testAssembly.Name, testAssembly.Tests.Count)));
                    var projectOutputFile = testAssembly.DllPath;
                    var testResultsFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "test.trx");
                    MSTestRunner.Run(processRunner, projectOutputFile, testResultsFile, args.TestSettingsPath, testAssembly.Tests, _testResults);
                    Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing, string.Format("{0} (parsing coverage results)", testAssembly.Name)));
                    var coverageFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "coverage.results.xml");
                    CodeCoverageService.ParseCoverageResults(coverageFile, tests, _coverageResults);
                }
            }
            finally
            {
                LastCheck = DateTime.Now;
                Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Idle));
                Messenger.Default.Send(new TestsResultsMessage(_testResults.GetTestResults().Values.ToList()));
                Messenger.Default.Send(new RefreshTaggerMessage());
            }
        }

        private static List<UnitTest> FilterTests(Document document, TestResults testsResults, CoverageResults coverageResults, List<UnitTest> suggestedTests)
        {
            if (testsResults.GetTestResults().Count == 0)
                return suggestedTests;

            testsResults.RemoveDeletedTests(suggestedTests);

            var impactedTests = coverageResults.GetImpactedTests(document.FullName);
            var oldTests = new HashSet<Guid>(testsResults.GetTestResults().Keys);

            return suggestedTests.Where(x => impactedTests.Contains(x.Id) || !oldTests.Contains(x.Id)).ToList();
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
            lock (_resetLock)
            {
                _queue.Clear();
                _coverageResults.Reset();
                _testResults.Reset();
            }
        }
    }

    internal class AnalysisArgs
    {
        public Solution Solution { get; set; }
        public Document Document { get; set; }
        public string TestSettingsPath { get; set; }
    }
}
