
using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace ClassSummaryInline
{
    internal static class SummaryHelper
    {
        /// <summary>
        /// 从 ClassDeclarationSyntax 的文档注释中提取 <summary> 的第一行（去除XML标记与多行空白）。
        /// </summary>
        public static string GetSummaryFirstLine(ClassDeclarationSyntax cls)
        {
            var trivia = cls.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
            if (trivia == default) return string.Empty;

            var structure = trivia.GetStructure() as DocumentationCommentTriviaSyntax;
            if (structure == null) return string.Empty;

            // 找到 <summary> 节点
            var summaryXml = structure.Content.OfType<XmlElementSyntax>()
                .FirstOrDefault(x => string.Equals(x.StartTag?.Name.LocalName.Text, "summary", StringComparison.OrdinalIgnoreCase));
            if (summaryXml == null)
                return string.Empty;

            var text = string.Concat(summaryXml.Content
                .OfType<XmlTextSyntax>()
                .SelectMany(x => x.TextTokens)
                .Select(t => t.Text));

            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // 取第一行，清理空白
            var firstLine = text.Replace("\r", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            return firstLine.Trim();
        }
    }
}
