using System;
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

        public static string GetMSTestPath()
        {
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%VS100COMNTOOLS%..\IDE\mstest.exe")))
                return Environment.ExpandEnvironmentVariables(@"%VS100COMNTOOLS%..\IDE\mstest.exe");
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%VS110COMNTOOLS%..\IDE\mstest.exe")))
                return Environment.ExpandEnvironmentVariables(@"%VS110COMNTOOLS%..\IDE\mstest.exe");
            if (File.Exists(Environment.ExpandEnvironmentVariables(@"%VS120COMNTOOLS%..\IDE\mstest.exe")))
                return Environment.ExpandEnvironmentVariables(@"%VS120COMNTOOLS%..\IDE\mstest.exe");
            return null;
        }

    }
}
