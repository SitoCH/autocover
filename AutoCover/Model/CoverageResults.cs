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
        private readonly Dictionary<string, HashSet<string>> _impactedTests = new Dictionary<string, HashSet<string>>();

        public void ProcessCodeBlock(string test, string document, CodeBlock cb)
        {
            //if (!_documents.ContainsKey(document))
            //    _documents.Add(document, new List<CodeBlock>());
            //_documents[document].Add(cb);

            if (!_impactedTests.ContainsKey(document))
                _impactedTests.Add(document, new HashSet<string>());
            _impactedTests[document].Add(test);
        }

        public void Clear()
        {
            _impactedTests.Clear();
        }

        public HashSet<string> GetImpactedTests(string document)
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
            return new HashSet<string>();
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
