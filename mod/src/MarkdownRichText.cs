using System;
using System.Text;
using System.Text.RegularExpressions;

namespace GreenHellAI
{
    // Converts the small, safe Markdown subset used by the companion into Unity IMGUI rich text.
    // User supplied text is escaped before tags are introduced, so arbitrary Unity tags cannot execute.
    internal static class MarkdownRichText
    {
        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Singleline);
        private static readonly Regex Italic = new Regex(@"(?<!\*)\*([^*\r\n]+?)\*(?!\*)", RegexOptions.Singleline);

        internal static string Render(string value)
        {
            if (String.IsNullOrEmpty(value)) return String.Empty;
            StringBuilder escaped = new StringBuilder(value.Length + 16);
            foreach (char c in value)
            {
                if (c == '&') escaped.Append("&amp;");
                else if (c == '<') escaped.Append("&lt;");
                else if (c == '>') escaped.Append("&gt;");
                else if (c == '"') escaped.Append("&quot;");
                else escaped.Append(c);
            }
            string result = Bold.Replace(escaped.ToString(), "<b>$1</b>");
            return Italic.Replace(result, "<i>$1</i>");
        }
    }
}
