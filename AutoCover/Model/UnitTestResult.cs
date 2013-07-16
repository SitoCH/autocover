using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCover
{
    public enum CodeCoverageResult
    {
        Passed, Failed, NotCovered
    }

    public enum UnitTestResult
    {
        Passed, Failed
    }

    [Serializable]
    public class UnitTest
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        
        public string ProjectName { get; set; }

        public string HumanReadableId { get; set; }

        public UnitTestResult Result { get; set; }

        public string Message { get; set; }
    }
}
