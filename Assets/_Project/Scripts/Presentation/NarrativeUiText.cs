using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    /// <summary>Shared typography for narrative and descriptive uGUI body copy.</summary>
    internal static class NarrativeUiText
    {
        internal const float LineSpacing = 1.4f;

        /// <summary>Thin space inserted between consecutive CJK characters (legacy Text has no tracking).</summary>
        private const char CjkLetterGap = '\u2009';

        internal static void ApplyBodyStyle(Text text)
        {
            if (text == null) return;
            text.lineSpacing = LineSpacing;
            text.fontStyle = FontStyle.Normal;
        }

        internal static void Set(Text text, string content)
        {
            if (text == null) return;
            text.text = FormatContent(content);
        }

        internal static string FormatContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            if (content.IndexOf('<') >= 0) return content;
            return AddCjkLetterSpacing(content);
        }

        private static string AddCjkLetterSpacing(string content)
        {
            var sb = new StringBuilder(content.Length + content.Length / 4);
            char prev = '\0';
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (prev != '\0' && IsCjk(prev) && IsCjk(c))
                    sb.Append(CjkLetterGap);
                sb.Append(c);
                prev = c;
            }

            return sb.ToString();
        }

        private static bool IsCjk(char c)
        {
            return (c >= 0x4E00 && c <= 0x9FFF)
                || (c >= 0x3400 && c <= 0x4DBF)
                || (c >= 0xF900 && c <= 0xFAFF);
        }
    }
}
