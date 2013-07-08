﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

    }
}