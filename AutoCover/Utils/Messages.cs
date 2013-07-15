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
        Idle, Building, Testing
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
