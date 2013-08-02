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
using System.Linq;
using System.Text;

namespace AutoCover
{
    public class TestsResultsMessage
    {
        public List<UnitTest> Tests { get; private set; }

        public TestsResultsMessage(List<UnitTest> tests)
        {
            Tests = tests;
        }
    }

    public enum AutoCoverEngineStatus
    {
        Idle, Building, Instrumenting, Testing
    }

    public class AutoCoverEngineStatusMessage
    {
        public AutoCoverEngineStatus Status { get; private set; }
        public string Message { get; private set; }

        public AutoCoverEngineStatusMessage(AutoCoverEngineStatus status)
        {
            Status = status;
        }

        public AutoCoverEngineStatusMessage(AutoCoverEngineStatus status, string message)
            : this(status)
        {
            Message = message;
        }
    }

    public class RefreshTaggerMessage
    {
    }

    public enum SolutionStatus
    {
        Opened, Closed
    }

    public class SolutionStatusChangedMessage
    {
        public SolutionStatus Status { get; private set; }

        public SolutionStatusChangedMessage(SolutionStatus status)
        {
            Status = status;
        }
    }
}
