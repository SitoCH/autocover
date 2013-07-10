using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.Common;

namespace AutoCover
{
    public class TestResults
    {
        private readonly Dictionary<Guid, UnitTest> _testResults = new Dictionary<Guid, UnitTest>();

        public void ProcessUnitTestResult(Guid testId, UnitTest result)
        {
            _testResults[testId] = result;
        }

        public Dictionary<Guid, UnitTest> GetTestResults()
        {
            return _testResults;
        }

        public void Reset()
        {
            _testResults.Clear();
        }

        public void RemoveDeletedTests(List<ITestElement> suggestedTests)
        {
            var newTests = new HashSet<Guid>(suggestedTests.Select(x => x.Id.Id));
            foreach (var key in _testResults.Keys.ToList())
            {
                if (!newTests.Contains(key))
                    _testResults.Remove(key);
            }
        }
    }
}
