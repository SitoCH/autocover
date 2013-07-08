using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCover
{
    public class CoverageResult
    {
        private readonly Dictionary<string, List<CodeBlock>> _documents = new Dictionary<string, List<CodeBlock>>();

        public List<CodeBlock> GetCodeBlocksFromDocument(string document)
        {
            if (!_documents.ContainsKey(document))
                _documents.Add(document, new List<CodeBlock>());
            return _documents[document];
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
