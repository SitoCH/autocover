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
        private static readonly CoverageResult _coverageResult = new CoverageResult();

        public static void CheckSolution(Solution solution, Document document, List<ITestElement> allTests, string testSettingsPath)
        {
            Task.Factory.StartNew(() =>
                {
                    lock (_lock)
                    {
                        var tests = _coverageResult.FilterTests(document, allTests);
                        if (tests.Count == 0)
                            return new List<UnitTest>();
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building));
                        // Build the tests projects
                        var testProjects = tests.Select(x => x.ProjectName).Distinct().ToList();
                        var testAssemblies = new List<TestAssembly>();
                        foreach (Project project in solution.Projects)
                        {
                            if (testProjects.Contains(project.Name))
                            {
                                Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Building, project.Name));
                                solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);
                                if (solution.SolutionBuild.LastBuildInfo != 0)
                                    return new List<UnitTest>();
                                var projectOutputFile = Instrument(project);
                                var ta = new TestAssembly { Name = project.Name, DllPath = projectOutputFile, Tests = tests.Where(x => x.ProjectData.ProjectName == project.Name).ToList() };
                                testAssemblies.Add(ta);

                                var coverageFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "coverage.xml");
                                File.Copy(coverageFile, coverageFile + ".clean", true);

                            }
                        }
                        Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing));
                        var msTestPathExe = Utils.GetMSTestPath();
                        var processRunner = new ProcessRunner(msTestPathExe, Path.GetDirectoryName(msTestPathExe));
                        foreach (var testAssembly in testAssemblies)
                        {
                            Messenger.Default.Send(new AutoCoverEngineStatusMessage(AutoCoverEngineStatus.Testing, testAssembly.Name));
                            var projectOutputFile = testAssembly.DllPath;
                            var testResultsFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "test.trx");
                            var coverageFile = Path.Combine(Path.GetDirectoryName(projectOutputFile), "coverage.xml");
                            File.Copy(coverageFile + ".clean", coverageFile, true);
                            TestMethod(processRunner, projectOutputFile, testResultsFile, testSettingsPath, testAssembly.Tests);
                            //ParseTests(testResultsFile, _coverageResult, test.HumanReadableId);
                            //ParseCoverageResults(coverageFile, _coverageResult, test.HumanReadableId);
                            File.Delete(testResultsFile);
                        }
                        return _coverageResult.GetTestResults();
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

        private static void ParseCoverageResults(string coverageFile, CoverageResult coverageResult, string test)
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
        }

        private static void ParseTests(string testResultsFile, CoverageResult coverageResult, string test)
        {
            using (var fileStreamReader = new StreamReader(testResultsFile))
            {
                var xmlSer = new XmlSerializer(typeof(TestRunType));
                var testRunType = (TestRunType)xmlSer.Deserialize(fileStreamReader);
                foreach (var itob1 in testRunType.Items) // Useless loop, every test has it's own file
                {
                    var resultsType = itob1 as ResultsType;
                    if (resultsType == null)
                        continue;
                    foreach (var itob2 in resultsType.Items)
                    {
                        var unitTestResultType = itob2 as UnitTestResultType;
                        if (unitTestResultType == null)
                            continue;

                        var unitTest = new UnitTest { Name = unitTestResultType.testName };
                        coverageResult.ProcessUnitTestResult(test, unitTest);
                        var outcome = unitTestResultType.outcome;
                        if (outcome != "Failed")
                            continue;
                        var items = unitTestResultType.Items;
                        if (items == null || items.Length <= 0)
                            continue;
                        // now we know we have a failed test; look for the desired string(s) in the error message
                        var outputType = (OutputType)unitTestResultType.Items[0];
                        var errorInfo = outputType.ErrorInfo;
                        var message = errorInfo.Message;
                        var text = ((XmlNode[])message)[0].InnerText;

                        unitTest.Result = UnitTestResult.Failed;
                        unitTest.Message = text;
                    }
                }
            }
        }

        private static void TestMethod(ProcessRunner runner, string projectDll, string testResultsFile, string testSettingsPath, List<ITestElement> tests)
        {
            var testsToRun = new StringBuilder();
            foreach (var testElement in tests)
            {
                testsToRun.Append(" /test:" + testElement.HumanReadableId);
            }
            var output = runner.Run(" /nologo /unique /noisolation /testcontainer:\"" + projectDll + "\" /resultsfile:\"" + testResultsFile + "\" /testsettings:\"" + testSettingsPath + "\" " + testsToRun);
        }

        private static IEnumerable<string> GetAssemblies(string fullPath)
        {
            return Directory.GetFiles(fullPath).Where(file => (Path.GetExtension(file) == ".dll" || Path.GetExtension(file) == ".exe") && File.Exists(Path.ChangeExtension(file, "pdb")));
        }
    }
}
