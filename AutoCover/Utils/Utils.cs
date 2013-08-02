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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.Text;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace AutoCover
{
    public static class Utils
    {
        private static readonly string[] msTestPathHints = new[]
            {
                Environment.ExpandEnvironmentVariables("%VS100COMNTOOLS%\\..\\IDE\\"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles(X86)%\\Microsoft Visual Studio 11.0\\Common7\\"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles(X86)%\\Microsoft Visual Studio 10.0\\Common7\\"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Visual Studio 11.0\\Common7\\"),
                Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Microsoft Visual Studio 10.0\\Common7\\"),
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\",
                "C:\\Program Files\\Microsoft Visual Studio 10.0\\Common7\\IDE\\",
                "C:\\Program Files (x86)\\Microsoft Visual Studio 11.0\\Common7\\IDE\\",
                "C:\\Program Files (x86)\\Microsoft Visual Studio 10.0\\Common7\\IDE\\"
            };

        public static string GetMSTestPath()
        {
            return SearchForExe("MSTest.exe");
        }

        private static string SearchForExe(string exe)
        {
            foreach (var alternative in msTestPathHints)
            {
                var msTestPath = Path.Combine(alternative, exe);
                if (File.Exists(Path.GetFullPath(msTestPath)))
                    return msTestPath;
            }
            throw new FileNotFoundException(string.Format("Could not locate {0}.", exe));
        }

        public static string GetDevEnvPath()
        {
            return SearchForExe("devenv.exe");
        }

        public static void Clear<T>(this BlockingCollection<T> blockingCollection)
        {
            if (blockingCollection == null)
            {
                throw new ArgumentNullException("blockingCollection");
            }

            while (blockingCollection.Count > 0)
            {
                T item;
                blockingCollection.TryTake(out item);
            }
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

    internal static class UnitTestIdHash
    {
        private static readonly HashAlgorithm s_provider = new SHA1CryptoServiceProvider();

        internal static HashAlgorithm Provider
        {
            get { return s_provider; }
        }

        /// 
        /// Calculates a hash of the string and copies the first 128 bits of the hash
        /// to a new Guid.
        /// 
        internal static Guid GuidFromString(this string data)
        {
            Debug.Assert(!String.IsNullOrEmpty(data));
            var hash = Provider.ComputeHash(Encoding.Unicode.GetBytes(data));

            // Guid is always 16 bytes
            Debug.Assert(Guid.Empty.ToByteArray().Length == 16, "Expected Guid to be 16 bytes");

            var toGuid = new byte[16];
            Array.Copy(hash, toGuid, 16);

            return new Guid(toGuid);
        }
    }
}