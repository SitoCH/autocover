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

        public Tuple<string, int> Run(string arguments)
        {
            var outputBuilder = new StringBuilder();
            _startInfo.Arguments = arguments;
            var proc = new Process { StartInfo = _startInfo };
            proc.OutputDataReceived += (sender, e) => outputBuilder.Append(e.Data);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            proc.CancelOutputRead();
            return new Tuple<string, int>(outputBuilder.ToString(), proc.ExitCode);
        }
    }
}
