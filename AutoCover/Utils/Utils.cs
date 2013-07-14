using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TestTools.Common;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace AutoCover
{
    public static class Utils
    {
        public static void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

            foreach (var directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        private static readonly string[] msTestPathHints = new[] { Environment.ExpandEnvironmentVariables("%VS100COMNTOOLS%\\..\\IDE\\MSTest.exe"),
                                            Environment.ExpandEnvironmentVariables("%ProgramFiles(X86)%\\Microsoft Visual Studio 11.0\\Common7\\MSTest.exe"),
                                            Environment.ExpandEnvironmentVariables("%ProgramFiles(X86)%\\Microsoft Visual Studio 10.0\\Common7\\MSTest.exe"),
                                            Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Visual Studio 11.0\\Common7\\MSTest.exe"),
                                            Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Visual Studio 10.0\\Common7\\MSTest.exe"),
                                            "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\MSTest.exe",
                                            "C:\\Program Files\\Microsoft Visual Studio 10.0\\Common7\\IDE\\MSTest.exe",
                                            "C:\\Program Files (x86)\\Microsoft Visual Studio 11.0\\Common7\\IDE\\MSTest.exe",
                                            "C:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\Common7\\IDE\\MSTest.exe"};

        public static string GetMSTestPath()
        {
            foreach (var alternative in msTestPathHints)
            {
                if (File.Exists(Path.GetFullPath(alternative)))
                    return alternative;
            }
            throw new FileNotFoundException("Could not locate MSTest.exe.");
        }

        public static ITextDocument GetTextDocument(this ITextBuffer TextBuffer)
        {
            ITextDocument textDoc;
            var rc = TextBuffer.Properties.TryGetProperty<ITextDocument>(
              typeof(ITextDocument), out textDoc);
            if (rc == true)
                return textDoc;
            else
                return null;
        }

        public static List<ITestElement> FilterTests(Document document, TestResults testsResults, CoverageResults coverageResults, List<ITestElement> suggestedTests)
        {
            if (testsResults.GetTestResults().Count == 0)
                return suggestedTests;

            testsResults.RemoveDeletedTests(suggestedTests);

            var impactedTests = coverageResults.GetImpactedTests(document.FullName);
            var oldTests = new HashSet<Guid>(testsResults.GetTestResults().Keys);

            return suggestedTests.Where(x => impactedTests.Contains(x.Id.Id) || !oldTests.Contains(x.Id.Id)).ToList();
        }

        public static string GetProjectTypeGuids(this Project proj)
        {

            string projectTypeGuids = "";
            object service = null;
            Microsoft.VisualStudio.Shell.Interop.IVsSolution solution = null;
            Microsoft.VisualStudio.Shell.Interop.IVsHierarchy hierarchy = null;
            Microsoft.VisualStudio.Shell.Interop.IVsAggregatableProject aggregatableProject = null;
            int result = 0;

            service = GetService(proj.DTE, typeof(Microsoft.VisualStudio.Shell.Interop.IVsSolution));
            solution = (Microsoft.VisualStudio.Shell.Interop.IVsSolution)service;

            result = solution.GetProjectOfUniqueName(proj.UniqueName, out hierarchy);

            if (result == 0)
            {
                aggregatableProject = (Microsoft.VisualStudio.Shell.Interop.IVsAggregatableProject)hierarchy;
                result = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);
            }

            return projectTypeGuids;

        }

        private static object GetService(object serviceProvider, System.Type type)
        {
            return GetService(serviceProvider, type.GUID);
        }

        private static object GetService(object serviceProviderObject, System.Guid guid)
        {

            object service = null;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null;
            IntPtr serviceIntPtr;
            int hr = 0;
            Guid SIDGuid;
            Guid IIDGuid;

            SIDGuid = guid;
            IIDGuid = SIDGuid;
            serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)serviceProviderObject;
            hr = serviceProvider.QueryService(ref SIDGuid, ref IIDGuid, out serviceIntPtr);

            if (hr != 0)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                service = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);
                System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
            }

            return service;
        }
    }
}
