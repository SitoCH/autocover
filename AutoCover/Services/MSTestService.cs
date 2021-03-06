﻿// Copyright (c) 2013
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoCover
{
    public static class MSTestService
    {
        public static void Run(ProcessRunner processRunner, string projectOutputFile, string testResultsFile, string testSettingsPath, List<ACUnitTest> tests, TestResults testResults)
        {
            ExecuteTests(processRunner, projectOutputFile, testResultsFile, testSettingsPath, tests);
            ParseTests(testResultsFile, testResults);
        }

        private static void ExecuteTests(ProcessRunner runner, string projectDll, string testResultsFile, string testSettingsPath, List<ACUnitTest> tests)
        {
            var testsToRun = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><TestLists xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><TestList name=\"AutoCover\" id=\"acdf4fe0-64ae-4a24-9c52-62e478f1e624\" parentListId=\"8c43106b-9dc1-4907-a29f-aa66a61bf5b6\">");

            using (var stringWriter = new StringWriter())
            {
                using (var xmlTextWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                {
                    var xmlDoc = new XmlDocument();
                    var e = xmlDoc.CreateElement("TestLinks");
                    xmlDoc.AppendChild(e);
                    foreach (var testElement in tests)
                    {
                        var testLink = xmlDoc.CreateElement("TestLink");
                        testLink.SetAttribute("id", testElement.Id.ToString("D"));
                        testLink.SetAttribute("name", testElement.HumanReadableId);
                        testLink.SetAttribute("storage", projectDll);
                        e.AppendChild(testLink);
                    }
                    xmlDoc.WriteTo(xmlTextWriter);
                    xmlTextWriter.Flush();
                    testsToRun.AppendLine(stringWriter.GetStringBuilder().ToString());
                }
            }

            testsToRun.AppendLine("</TestList></TestLists>");

            var testListFile = Path.Combine(Path.GetDirectoryName(testResultsFile), "autocover.vsmdi");
            File.WriteAllText(testListFile, testsToRun.ToString());

            runner.Run(" /nologo /resultsfile:\"" + testResultsFile + "\" /testsettings:\"" + testSettingsPath + "\"  /testmetadata:\"" + testListFile + "\" /testlist:AutoCover");
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

                        var unitTest = new ACUnitTest { Name = unitTestResultType.testName };
                        testResults.ProcessUnitTestResult(new Guid(unitTestResultType.testId), unitTest);
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

        public static ICollection<ACUnitTest> GetTests(string projectName, string projectOutputFile)
        {
            var appDomain = AppDomain.CreateDomain("AppCoverDomain", null, new AppDomainSetup { ApplicationBase = Environment.CurrentDirectory, LoaderOptimization = LoaderOptimization.MultiDomainHost });
            try
            {
                var obj = new AppCoverBoundaryObject(projectName, projectOutputFile);
                appDomain.DoCallBack(obj.ParseTests);
                var serializer = new XmlSerializer(typeof(List<ACUnitTest>));
                using (var textWriter = new StreamReader(projectOutputFile + ".xml"))
                {
                    return (ICollection<ACUnitTest>)serializer.Deserialize(textWriter);
                }
            }
            catch
            {
                return new List<ACUnitTest>();
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }
    }

    [Serializable]
    class AppCoverBoundaryObject
    {
        private readonly string _projectName;
        private readonly string _projectOutputFile;

        public AppCoverBoundaryObject(string projectName, string projectOutputFile)
        {
            _projectName = projectName;
            _projectOutputFile = projectOutputFile;
        }

        public void ParseTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            var assemlby = AppDomain.CurrentDomain.Load(File.ReadAllBytes(_projectOutputFile));
            var tests = assemlby.GetTypes().SelectMany(x => x.GetMethods())
                 .Where(x => x.IsDefined(typeof(TestMethodAttribute), true))
                 .Select(method => new { method, humanReadableId = string.Format("{0}.{1}.{2}", method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name) })
                 .Select(@t => new { @t, id = @t.humanReadableId.GuidFromString() })
                 .Select(@t => new ACUnitTest { Id = @t.id, HumanReadableId = @t.@t.humanReadableId, Name = @t.@t.method.Name, ProjectName = _projectName }).ToList();

            var serializer = new XmlSerializer(typeof(List<ACUnitTest>));
            using (var textWriter = new StreamWriter(_projectOutputFile + ".xml"))
            {
                serializer.Serialize(textWriter, tests);
            }

            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = args.Name;
            var pos = assemblyName.IndexOf(',');
            if (pos > -1)
                assemblyName = assemblyName.Substring(0, pos);

            var basePath = Path.GetDirectoryName(_projectOutputFile);
            var dllPath = Path.Combine(basePath, assemblyName + ".dll");
            if (File.Exists(dllPath))
                return Assembly.LoadFile(dllPath);
            var exePath = Path.Combine(basePath, assemblyName + ".exe");
            if (File.Exists(exePath))
                return Assembly.LoadFile(exePath);
            return null;
        }
    }

}
