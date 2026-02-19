
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClassSummaryInline
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal sealed class ClassSummaryAdornmentProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer != buffer) return null;
            return (ITagger<T>)new ClassSummaryTagger((IWpfTextView)textView, buffer);
        }
    }

    internal sealed class ClassSummaryTagger : ITagger<IntraTextAdornmentTag>
    {
        private readonly IWpfTextView _view;
        private readonly ITextBuffer _buffer;
        private readonly DispatcherTimer _debounce;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public ClassSummaryTagger(IWpfTextView view, ITextBuffer buffer)
        {
            _view = view; _buffer = buffer;
            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _debounce.Tick += (s, e) => { _debounce.Stop(); RaiseAll(); };
            _buffer.Changed += (s, e) => _debounce.Start();
            RaiseAll();
        }

        private void RaiseAll()
        {
            var snapshot = _buffer.CurrentSnapshot;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0) yield break;
            var snapshot = spans[0].Snapshot;
            var text = snapshot.GetText();

            // 解析语法树（仅语法，不做编译绑定，性能更稳）
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                string summary = SummaryHelper.GetSummaryFirstLine(cls);
                if (string.IsNullOrWhiteSpace(summary)) continue;

                // 锚点：类名 token 结束位置（零宽度 span）
                var pos = cls.Identifier.Span.End;
                if (pos < 0 || pos > snapshot.Length) continue;
                var span = new SnapshotSpan(snapshot, pos, 0);

                var tb = new TextBlock
                {
                    Text = " // 摘要: " + summary,
                    Foreground = Brushes.Gray,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                };

                yield return new TagSpan<IntraTextAdornmentTag>(span, new IntraTextAdornmentTag(tb, null));
            }
        }
    }
}
