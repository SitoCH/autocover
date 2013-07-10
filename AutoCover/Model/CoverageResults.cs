using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.Common;
using EnvDTE;

namespace AutoCover
{
    public class CoverageResults
    {
        //private readonly Dictionary<string, List<CodeBlock>> _documents = new Dictionary<string, List<CodeBlock>>();
        private readonly Dictionary<string, HashSet<Guid>> _impactedTests = new Dictionary<string, HashSet<Guid>>();

        public void ProcessCodeBlock(Guid testId, string document, CodeBlock cb)
        {
            //if (!_documents.ContainsKey(document))
            //    _documents.Add(document, new List<CodeBlock>());
            //_documents[document].Add(cb);

            if (!_impactedTests.ContainsKey(document))
                _impactedTests.Add(document, new HashSet<Guid>());
            _impactedTests[document].Add(testId);
        }

        public void Reset()
        {
            _impactedTests.Clear();
        }

        public HashSet<Guid> GetImpactedTests(string document)
        {
            if (_impactedTests.ContainsKey(document))
            {
                var impactedTests = _impactedTests[document];
                foreach (var test in impactedTests)
                {
                    //_documents.Remove(test);
                }
                return impactedTests;
            }
            return new HashSet<Guid>();
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
