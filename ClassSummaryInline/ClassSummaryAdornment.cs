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

// 为 Editor 命名空间起别名，统一使用 Editor.IntraTextAdornmentTag
using Editor = Microsoft.VisualStudio.Text.Editor;

namespace ClassSummaryInline
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    // 关键：TagType 显式绑定 Editor.IntraTextAdornmentTag
    [TagType(typeof(Editor.IntraTextAdornmentTag))]
    internal sealed class ClassSummaryAdornmentProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // 仅对当前视图主缓冲区
            if (textView.TextBuffer != buffer) return null;

            // 仅当请求的 tag 类型是 Editor.IntraTextAdornmentTag 时返回实例
            if (typeof(T) != typeof(Editor.IntraTextAdornmentTag)) return null;

            return new ClassSummaryTagger((IWpfTextView)textView, buffer) as ITagger<T>;
        }
    }

    // 统一实现 ITagger<Editor.IntraTextAdornmentTag>
    internal sealed class ClassSummaryTagger : ITagger<Editor.IntraTextAdornmentTag>
    {
        private readonly IWpfTextView _view;
        private readonly ITextBuffer _buffer;
        private readonly DispatcherTimer _debounce;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public ClassSummaryTagger(IWpfTextView view, ITextBuffer buffer)
        {
            _view = view;
            _buffer = buffer;

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

        public IEnumerable<ITagSpan<Editor.IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0) yield break;

            var snapshot = spans[0].Snapshot;
            var text = snapshot.GetText();

            // 仅语法解析，性能更稳
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                string summary = SummaryHelper.GetSummaryFirstLine(cls);
                if (string.IsNullOrWhiteSpace(summary)) continue;

                // 锚点：类名 token 结束位置的零宽 span
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

                yield return new TagSpan<Editor.IntraTextAdornmentTag>(
                    span,
                    new Editor.IntraTextAdornmentTag(tb, null));
            }
        }
    }
}
