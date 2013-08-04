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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using EnvDTE;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.VisualStudio.Shell;

namespace AutoCover
{
    public static class AutoCoverService
    {
        private static readonly ConcurrentBag<AnalysisItem> _pendingFiles = new ConcurrentBag<AnalysisItem>();

        private static readonly CoverageResults _coverageResults = new CoverageResults();
        private static readonly TestResults _testResults = new TestResults();
        public static DateTime LastCheck { get; private set; }

        public static void AddDocument(Solution solution, Document document, string testSettingsPath)
        {
            if (SettingsService.Settings.EnableAutoCover)
                _pendingFiles.Add(new AnalysisItem { Solution = solution, Document = document, TestSettingsPath = testSettingsPath });
        }

        public static void InitEngine()
        {
            System.Threading.Tasks.Task.Factory.StartNew(Loop, TaskCreationOptions.LongRunning);
        }

        private static void Loop()
        {
            while (true)
            {
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                var items = TakeAllPendingItems().Where(x => x.Solution.FullName == dte.Solution.FullName).Distinct().ToList();
                if (ShouldProcessDocuments(dte, items))
                {
                    ProcessDocuments(dte.Solution, items);
                }
                else
                {
                    System.Threading.Thread.Sleep(2500);
                }
            }
        }

        private static bool ShouldProcessDocuments(DTE dte, ICollection<AnalysisItem> items)
        {
            if (items.Count == 0) // There are no valid documents to process
                return false;
            if (!SettingsService.Settings.EnableAutoCover) // AutoCover isn't enabled on this solution
                return false;
            if (dte.Solution == null) // No solution found
                return false;
            if (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress) // Visual Studio is already building something
                return false;
            if (dte.Debugger != null && dte.Debugger.DebuggedProcesses.Count > 0) // The debugger is attched to a process
                return false;
            return true;
        }

        private static IEnumerable<AnalysisItem> TakeAllPendingItems()
        {
            var files = new List<AnalysisItem>();
            while (!_pendingFiles.IsEmpty)
            {
                AnalysisItem item;
                if (_pendingFiles.TryTake(out item))
                {
                    files.Add(item);
                }
            }
            return files;
        }

        private static void ProcessDocuments(Solution solution, List<AnalysisItem> items)
        {
            try
            {
                Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building));
                // Build the tests projects
                var testAssemblies = new List<TestAssembly>();
                var suggestedTests = new List<ACUnitTest>();
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
                            suggestedTests.AddRange(MSTestService.GetTests(project.Name, projectOutputFile));
                            testAssemblies.Add(ta);
                        }
                    }
                }
                if (testAssemblies.Count == 0)
                    return;
                // Get all the impacted tests
                var tests = new List<ACUnitTest>();
                foreach (var item in items)
                {
                    tests.AddRange(FilterTests(item.Document.FullName, _testResults, _coverageResults, suggestedTests));
                }
                tests = tests.Distinct().ToList();
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
                    MSTestService.Run(processRunner, projectOutputFile, testResultsFile, items.First().TestSettingsPath, testAssembly.Tests, _testResults);
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

        private static IEnumerable<ACUnitTest> FilterTests(string documentPath, TestResults testsResults, CoverageResults coverageResults, List<ACUnitTest> suggestedTests)
        {
            if (testsResults.GetTestResults().Count == 0)
                return suggestedTests;

            testsResults.RemoveDeletedTests(suggestedTests);

            var impactedTests = coverageResults.GetImpactedTests(documentPath);
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
            TakeAllPendingItems();
            _coverageResults.Reset();
            _testResults.Reset();
            Messenger.Default.Send(new TestsResultsMessage(new List<ACUnitTest>()));
            Messenger.Default.Send(new RefreshTaggerMessage());
        }
    }

    internal class AnalysisItem : IEquatable<AnalysisItem>
    {
        public Solution Solution { get; set; }
        public Document Document { get; set; }
        public string TestSettingsPath { get; set; }

        public bool Equals(AnalysisItem other)
        {
            return Document.FullName == other.Document.FullName;
        }
    }
}
