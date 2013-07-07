using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCover
{ 
    public enum UnitTestResult
    {
        Passed, Failed 
    }

    public class UnitTest
    {
        public string Name { get; set; }

        public UnitTestResult Result { get; set; }

        public string Message { get; set; }
    }
}
