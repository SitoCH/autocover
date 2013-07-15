using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace AutoCover
{
    [ContentType("any")]
    [TagType(typeof(ClassificationTag))]
    [Export(typeof(IViewTaggerProvider))]
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

            var codeCoveragePassedBackgroundType = Registry.GetClassificationType("code-coverage-passed-covered");
            var codeCoverageFailedBackgroundType = Registry.GetClassificationType("code-coverage-failed-covered");
            return new CodeCoveragePassedTagger(textView, codeCoveragePassedBackgroundType, codeCoverageFailedBackgroundType) as ITagger<T>;
        }
    }

    public static class TypeExports
    {
        [Name("code-coverage-passed-covered")]
        [Export(typeof(ClassificationTypeDefinition))]
        public static ClassificationTypeDefinition CodeCoveragePassedBackground;

        [Name("code-coverage-failed-covered")]
        [Export(typeof(ClassificationTypeDefinition))]
        public static ClassificationTypeDefinition CodeCoverageFailedBackground;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "code-coverage-passed-covered")]
    [Name("code-coverage-passed-covered")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    public sealed class CodeCoveragePassedBackground : ClassificationFormatDefinition
    {
        public CodeCoveragePassedBackground()
        {
            DisplayName = "Code coverage passed background";
            BackgroundColor = Colors.LightGreen;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "code-coverage-failed-covered")]
    [Name("code-coverage-failed-covered")]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    public sealed class CodeCoverageFailedBackground : ClassificationFormatDefinition
    {
        public CodeCoverageFailedBackground()
        {
            DisplayName = "Code coverage failed background";
            BackgroundColor = Colors.LightCoral;
        }
    }

    public sealed class CodeCoveragePassedTagger : ITagger<ClassificationTag>
    {
        private readonly string _filePath;
        private readonly IClassificationType _codeCoveragePassedBackgroundType, _codeCoverageFailedBackgroundType;
        private readonly ITextView _view;
        private NormalizedSnapshotSpanCollection _currentPassedSpans, _currentFailedSpans;

        public CodeCoveragePassedTagger(ITextView view, IClassificationType codeCoveragePassedBackgroundType, IClassificationType codeCoverageFailedBackgroundType)
        {
            _view = view;
            _filePath = view.TextBuffer.GetTextDocument().FilePath;
            _codeCoveragePassedBackgroundType = codeCoveragePassedBackgroundType;
            _codeCoverageFailedBackgroundType = codeCoverageFailedBackgroundType;

            RefreshSpans(_view.TextSnapshot);
            _currentPassedSpans = new NormalizedSnapshotSpanCollection();
            _currentFailedSpans = new NormalizedSnapshotSpanCollection();

            _view.GotAggregateFocus += SetupSelectionChangedListener;
            _view.Closed += _view_Closed;
            Messenger.Default.Register<RefreshTaggerMessage>(this, m => RefreshSnapShot(_view.TextSnapshot));
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged = delegate { };

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection originalSpans)
        {
            if (originalSpans == null || originalSpans.Count == 0)
                yield break;

            if (_currentPassedSpans.Any())
            {
                var snapshot = _currentPassedSpans[0].Snapshot;
                var spans = new NormalizedSnapshotSpanCollection(originalSpans.Select(s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive)));

                foreach (var span in NormalizedSnapshotSpanCollection.Intersection(_currentPassedSpans, spans))
                {
                    yield return new TagSpan<ClassificationTag>(span, new ClassificationTag(_codeCoveragePassedBackgroundType));
                }
            }
            if (_currentFailedSpans.Any())
            {
                var snapshot = _currentFailedSpans[0].Snapshot;
                var spans = new NormalizedSnapshotSpanCollection(originalSpans.Select(s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive)));

                foreach (var span in NormalizedSnapshotSpanCollection.Intersection(_currentFailedSpans, spans))
                {
                    yield return new TagSpan<ClassificationTag>(span, new ClassificationTag(_codeCoverageFailedBackgroundType));
                }
            }
        }

        private void _view_Closed(object sender, EventArgs e)
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
            var document = snapshot.TextBuffer.GetTextDocument();
            if (document == null || document.LastContentModifiedTime > AutoCoverEngine.LastCheck || SettingsService.Settings.DisableRowHighlighting)
            {
                _currentPassedSpans = new NormalizedSnapshotSpanCollection();
                _currentFailedSpans = new NormalizedSnapshotSpanCollection();
                return;
            }

            var passedSpans = new List<SnapshotSpan>();
            var failedSpans = new List<SnapshotSpan>();
            foreach (var line in snapshot.Lines)
            {
                var status = AutoCoverEngine.GetLineResult(_filePath, line.LineNumber + 1);
                if (status == CodeCoverageResult.Passed)
                    passedSpans.Add(line.Extent);
                else if (status == CodeCoverageResult.Failed)
                    failedSpans.Add(line.Extent);
            }
            _currentPassedSpans = new NormalizedSnapshotSpanCollection(passedSpans);
            _currentFailedSpans = new NormalizedSnapshotSpanCollection(failedSpans);
        }
    }
}