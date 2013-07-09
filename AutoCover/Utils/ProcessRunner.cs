using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AutoCover
{
    public class ProcessRunner
    {
        private readonly ProcessStartInfo _startInfo;

        public ProcessRunner(string exe, string workingDir)
        {
            _startInfo = new ProcessStartInfo { WorkingDirectory = workingDir, FileName = exe, UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
        }

        public string Run(string arguments)
        {
            var outputBuilder = new StringBuilder();
            _startInfo.Arguments = arguments;
            var proc = new Process { StartInfo = _startInfo };
            proc.OutputDataReceived += (sender, e) => outputBuilder.Append(e.Data);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            proc.CancelOutputRead();
            return outputBuilder.ToString();
        }
    }
}
