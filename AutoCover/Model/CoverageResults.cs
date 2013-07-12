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
        // Document path -> code blocks
        private readonly Dictionary<string, Dictionary<CodeBlock, HashSet<Guid>>> _documents = new Dictionary<string, Dictionary<CodeBlock, HashSet<Guid>>>(StringComparer.OrdinalIgnoreCase);
        // Document path -> tests ids
        private readonly Dictionary<string, HashSet<Guid>> _impactedTests = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);

        public void ProcessCodeBlock(Guid testId, string document, CodeBlock cb)
        {
            if (!_documents.ContainsKey(document))
                _documents[document] = new Dictionary<CodeBlock, HashSet<Guid>>();
            if (!_documents[document].ContainsKey(cb))
                _documents[document][cb] = new HashSet<Guid>();
            _documents[document][cb].Add(testId);

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
                _documents.Remove(document);
                return impactedTests;
            }
            return new HashSet<Guid>();
        }

        public ICollection<Guid> GetTestsFor(string document, int line)
        {
            if (!_documents.ContainsKey(document))
                return new List<Guid>();
            return _documents[document].FirstOrDefault(x => line >= x.Key.Line && line <= x.Key.EndLine).Value;
        }
    }

    public class CodeBlock
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }
}
