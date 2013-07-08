using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.Common;
using EnvDTE;

namespace AutoCover
{
    public class CoverageResult
    {
        //private readonly Dictionary<string, List<CodeBlock>> _documents = new Dictionary<string, List<CodeBlock>>();
        private readonly Dictionary<string, HashSet<string>> _impactedTests = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, UnitTest> _testResults = new Dictionary<string, UnitTest>();


        internal void ProcessCodeBlock(string test, string document, CodeBlock cb)
        {
            //if (!_documents.ContainsKey(document))
            //    _documents.Add(document, new List<CodeBlock>());
            //_documents[document].Add(cb);

            if (!_impactedTests.ContainsKey(document))
                _impactedTests.Add(document, new HashSet<string>());
            _impactedTests[document].Add(test);
        }

        internal void ProcessUnitTestResult(string test, UnitTest result)
        {
            _testResults[test] = result;
        }

        public List<UnitTest> GetTestResults()
        {
            return _testResults.Values.ToList();
        }

        internal List<ITestElement> FilterTests(Document document, List<ITestElement> tests)
        {
            if (_testResults.Count == 0)
                return tests;
            var filteredTests = new List<ITestElement>();
            // Remove deleted tests
            var newTests = new HashSet<string>(tests.Select(x => x.HumanReadableId));
            foreach (var key in _testResults.Keys.ToList())
            {
                if (!newTests.Contains(key))
                    _testResults.Remove(key);
            }
            if (_impactedTests.ContainsKey(document.FullName))
            {
                var impactedTests = _impactedTests[document.FullName];
                foreach (var test in impactedTests)
                {
                    //_documents.Remove(test);
                }
                var oldTests = new HashSet<string>(_testResults.Keys);
                return tests.Where(x => impactedTests.Contains(x.HumanReadableId) || !oldTests.Contains(x.HumanReadableId)).ToList();
            }
            //_documents.Clear();
            _impactedTests.Clear();
            _testResults.Clear();
            return tests;
        }
    }

    public class CodeBlock
    {
        public int VisitCount { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }
}
