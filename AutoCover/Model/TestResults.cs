using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.Common;

namespace AutoCover
{
    public class TestResults
    {
        private readonly Dictionary<string, UnitTest> _testResults = new Dictionary<string, UnitTest>();

        public void ProcessUnitTestResult(string test, UnitTest result)
        {
            _testResults[test] = result;
        }

        public Dictionary<string, UnitTest> GetTestResults()
        {
            return _testResults;
        }

        public void Clear()
        {
            _testResults.Clear();
        }

        internal void RemoveDeletedStests(List<ITestElement> suggestedTests)
        {
            var newTests = new HashSet<string>(suggestedTests.Select(x => x.HumanReadableId));
            foreach (var key in _testResults.Keys.ToList())
            {
                if (!newTests.Contains(key))
                    _testResults.Remove(key);
            }
        }
    }
}
