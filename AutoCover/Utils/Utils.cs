using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.Text;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace AutoCover
{
    public static class Utils
    {
        private static readonly string[] msTestPathHints = new[]
            {
                Environment.ExpandEnvironmentVariables("%VS100COMNTOOLS%\\..\\IDE\\MSTest.exe"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles(X86)%\\Microsoft Visual Studio 11.0\\Common7\\MSTest.exe"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles(X86)%\\Microsoft Visual Studio 10.0\\Common7\\MSTest.exe"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Visual Studio 11.0\\Common7\\MSTest.exe"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Visual Studio 10.0\\Common7\\MSTest.exe"),
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\MSTest.exe",
                "C:\\Program Files\\Microsoft Visual Studio 10.0\\Common7\\IDE\\MSTest.exe",
                "C:\\Program Files (x86)\\Microsoft Visual Studio 11.0\\Common7\\IDE\\MSTest.exe",
                "C:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\Common7\\IDE\\MSTest.exe"
            };

        public static string GetMSTestPath()
        {
            foreach (string alternative in msTestPathHints)
            {
                if (File.Exists(Path.GetFullPath(alternative)))
                    return alternative;
            }
            throw new FileNotFoundException("Could not locate MSTest.exe.");
        }

        public static void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

            foreach (string directory in Directory.GetDirectories(sourceDir))
                Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
        }

        public static ITextDocument GetTextDocument(this ITextBuffer TextBuffer)
        {
            ITextDocument textDoc;
            var rc = TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out textDoc);
            return rc ? textDoc : null;
        }

        public static string GetProjectTypeGuids(this Project proj)
        {
            var projectTypeGuids = "";
            IVsHierarchy hierarchy;

            var service = GetService(proj.DTE, typeof(IVsSolution));
            var solution = (IVsSolution)service;

            var result = solution.GetProjectOfUniqueName(proj.UniqueName, out hierarchy);

            if (result == 0)
            {
                var aggregatableProject = hierarchy as IVsAggregatableProject;
                if (aggregatableProject != null)
                    aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);
            }

            return projectTypeGuids;
        }

        private static object GetService(object serviceProvider, Type type)
        {
            return GetService(serviceProvider, type.GUID);
        }

        private static object GetService(object serviceProviderObject, Guid guid)
        {
            object service = null;
            IntPtr serviceIntPtr;

            var SIDGuid = guid;
            var IIDGuid = SIDGuid;
            var serviceProvider = (IServiceProvider)serviceProviderObject;
            var hr = serviceProvider.QueryService(ref SIDGuid, ref IIDGuid, out serviceIntPtr);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                service = Marshal.GetObjectForIUnknown(serviceIntPtr);
                Marshal.Release(serviceIntPtr);
            }

            return service;
        }
    }
}