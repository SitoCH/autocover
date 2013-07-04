using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EnvDTE;
using Coverage.Common;
using Coverage.Report;
using Coverage.Instrument;
using Coverage;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml;

namespace AutoCover
{
    public static class SolutionRunner
    {
        public static void CheckSolution(Solution solution)
        {
            var lastBuildState = solution.SolutionBuild.LastBuildInfo;
            if (lastBuildState == 0)
            {
                var tempFolder = Path.Combine(Path.GetTempPath(), "AutoCover", Path.GetFileNameWithoutExtension(solution.FullName));
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
                Directory.CreateDirectory(tempFolder);
                foreach (Project project in solution.Projects)
                {
                    if (project.Name.Contains("Test")) // TODO Detect the right project type
                    {
                        var testPath = Path.Combine(tempFolder, project.Name);
                        InstrumentAndTest(project, testPath);
                        ParseTests(testPath);
                    }
                }
            }
        }

        private static void ParseTests(string testPath)
        {
            var fileInfo = new FileInfo(Path.Combine(testPath, "test.trx"));
            var fileStreamReader = new StreamReader(fileInfo.FullName);
            var xmlSer = new XmlSerializer(typeof(TestRunType));
            var testRunType = (TestRunType)xmlSer.Deserialize(fileStreamReader);

            foreach (var itob1 in testRunType.Items)
            {
                var resultsType = itob1 as ResultsType;

                if (resultsType == null)
                    continue;
                foreach (var itob2 in resultsType.Items)
                {
                    var unitTestResultType = itob2 as UnitTestResultType;
                    if (unitTestResultType == null)
                        continue;

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
                    throw new Exception(text);
                }
            }
        }

        private static void InstrumentAndTest(Project project, string testPath)
        {
            var basePath = project.Properties.Item("FullPath").Value.ToString();
            var outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            var dllsPath = Path.Combine(basePath, outputPath);
            Utils.Copy(dllsPath, testPath);
            Runner.Run(testPath, GetAssemblies(testPath));

            var outputBuilder = new StringBuilder();
            var pInfo = new ProcessStartInfo();
            pInfo.FileName = Utils.GetMSTestPath();
            pInfo.Arguments = " /testcontainer:" + project.Name + ".dll /resultsfile:test.trx"; // TODO Get the righe assembly name
            pInfo.WorkingDirectory = testPath;
            pInfo.RedirectStandardOutput = true;
            pInfo.UseShellExecute = false;
            pInfo.CreateNoWindow = true;

            var proc = new System.Diagnostics.Process();
            proc.StartInfo = pInfo;
            proc.OutputDataReceived += new DataReceivedEventHandler(delegate(object sender, DataReceivedEventArgs e)
            {
                outputBuilder.Append(e.Data);
            });
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            proc.CancelOutputRead();
            var output = outputBuilder.ToString();
        }

        private static IEnumerable<string> GetAssemblies(string fullPath)
        {
            return Directory.GetFiles(fullPath).Where(file => (Path.GetExtension(file) == ".dll" || Path.GetExtension(file) == ".exe"));
        }
    }
}
