// Copyright (c) 2013
// Simone Grignola [http://www.grignola.ch]
//
// This file is part of AutoCover.
//
// AutoCover is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
                _impactedTests.Remove(document);
                foreach (var otherDocument in _documents)
                {
                    var toRemove = new List<CodeBlock>();
                    foreach (var cb in otherDocument.Value)
                    {
                        cb.Value.RemoveWhere(x => impactedTests.Contains(x));
                        if (cb.Value.Count == 0)
                            toRemove.Add(cb.Key);
                    }
                    foreach (var cbToRemove in toRemove)
                        otherDocument.Value.Remove(cbToRemove);
                }
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
