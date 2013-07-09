using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.Common;

namespace AutoCover
{
    public static class MSTestRunner
    {
        public static void Run(ProcessRunner processRunner, string projectOutputFile, string testResultsFile, string testSettingsPath, List<ITestElement> tests, TestResults testResults)
        {
            ExecuteTests(processRunner, projectOutputFile, testResultsFile, testSettingsPath, tests);
            ParseTests(testResultsFile, testResults);
        }

        private static void ExecuteTests(ProcessRunner runner, string projectDll, string testResultsFile, string testSettingsPath, List<ITestElement> tests)
        {
            var testsToRun = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><TestLists xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><TestList name=\"AutoCover\" id=\"acdf4fe0-64ae-4a24-9c52-62e478f1e624\" parentListId=\"8c43106b-9dc1-4907-a29f-aa66a61bf5b6\">");

            using (var stringWriter = new StringWriter())
            {
                using (var xmlTextWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    var e = xmlDoc.CreateElement("TestLinks");
                    xmlDoc.AppendChild(e);
                    foreach (var testElement in tests)
                    {
                        testElement.Storage = projectDll;
                        var xmlTest = xmlDoc.CreateElement("TestLink");
                        xmlDoc.DocumentElement.AppendChild(xmlTest);
                        testElement.Link.Save(xmlTest, null);
                    }
                    xmlDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    testsToRun.AppendLine(stringWriter.GetStringBuilder().ToString());
                }
            }

            testsToRun.AppendLine("</TestList></TestLists>");

            var testListFile = Path.Combine(Path.GetDirectoryName(testResultsFile), "autocover.vsmdi");
            File.WriteAllText(testListFile, testsToRun.ToString());

            var output = runner.Run(" /nologo /noisolation /resultsfile:\"" + testResultsFile + "\" /testsettings:\"" + testSettingsPath + "\"  /testmetadata:\"" + testListFile + "\" /testlist:AutoCover");
        }

        private static void ParseTests(string testResultsFile, TestResults testResults)
        {
            using (var fileStreamReader = new StreamReader(testResultsFile))
            {
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

                        var unitTest = new UnitTest { Name = unitTestResultType.testName };
                        testResults.ProcessUnitTestResult(unitTestResultType.testId, unitTest);
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
    }
}
