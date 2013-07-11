using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using GalaSoft.MvvmLight.Messaging;


namespace AutoCover
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(ClassificationTag))]
    public sealed class CodeCoverageTaggerProvider : IViewTaggerProvider
    {
        [Import]
        public IClassificationTypeRegistryService Registry;

        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer != textView.TextBuffer || textView.TextBuffer.GetTextDocument() == null)
                return null;

            var classType = Registry.GetClassificationType("code-coverage-covered");
            return new CodeCoverageTagger(textView, TextSearchService, classType) as ITagger<T>;
        }
    }

    public static class TypeExports
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("code-coverage-covered")]
        public static ClassificationTypeDefinition OrdinaryClassificationType;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "code-coverage-covered")]
    [Name("code-coverage-covered")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    public sealed class CodeCoverageBackground : ClassificationFormatDefinition
    {
        public CodeCoverageBackground()
        {
            DisplayName = "Code coverage background";
            BackgroundColor = Colors.LightGreen;
        }
    }

    public sealed class CodeCoverageTagger : ITagger<ClassificationTag>
    {
        private readonly ITextView _view;
        private readonly string _filePath;
        private readonly ITextSearchService _searchService;
        private readonly IClassificationType _type;
        private NormalizedSnapshotSpanCollection _currentSpans;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged = delegate { };

        public CodeCoverageTagger(ITextView view, ITextSearchService searchService, IClassificationType type)
        {
            _view = view;
            _filePath = view.TextBuffer.GetTextDocument().FilePath;
            _searchService = searchService;
            _type = type;

            RefreshSpans(_view.TextSnapshot);
            _currentSpans = new NormalizedSnapshotSpanCollection();
            _view.GotAggregateFocus += SetupSelectionChangedListener;
            _view.Closed += _view_Closed;

            Messenger.Default.Register<RefreshTaggerMessage>(this, m => RefreshSnapShot(_view.TextSnapshot));
        }

        void _view_Closed(object sender, EventArgs e)
        {
            Messenger.Default.Unregister<RefreshTaggerMessage>(this);
        }

        private void SetupSelectionChangedListener(object sender, EventArgs e)
        {
            if (_view != null)
            {
                _view.LayoutChanged += ViewLayoutChanged;
                _view.GotAggregateFocus -= SetupSelectionChangedListener;
            }
        }

        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.OldSnapshot != e.NewSnapshot)
            {
                RefreshSnapShot(e.NewSnapshot);
            }
        }

        private void RefreshSnapShot(ITextSnapshot snapshot)
        {
            RefreshSpans(snapshot);
            TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        private void RefreshSpans(ITextSnapshot snapshot)
        {
            _currentSpans = new NormalizedSnapshotSpanCollection(snapshot.Lines.Where(x => AutoCoverEngine.IsLineCovered(_filePath, x.LineNumber)).Select(x => x.Extent));
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0 || _currentSpans.Count == 0)
                yield break;

            var snapshot = _currentSpans[0].Snapshot;
            spans = new NormalizedSnapshotSpanCollection(spans.Select(s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive)));

            foreach (var span in NormalizedSnapshotSpanCollection.Intersection(_currentSpans, spans))
            {
                yield return new TagSpan<ClassificationTag>(span, new ClassificationTag(_type));
            }
        }
    }
}
