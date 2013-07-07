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
        Idle, Running
    }

    public class AutoCoverEngineStatusMessage
    {
        public AutoCoverEngineStatus Status { get; private set; }

        public AutoCoverEngineStatusMessage(AutoCoverEngineStatus status)
        {
            Status = status;
        }
    }
}
