using System;
using System.Text.RegularExpressions;

namespace Recordaudio.Services
{
    public sealed class SimpleTranscriptAnonymizer
    {
        public string Anonymize(string text, string clientName)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string result = text;

            result = ReplaceClientName(result, clientName);
            result = ReplaceEmails(result);
            result = ReplacePhones(result);
            result = ReplaceUrls(result);
            result = ReplaceIbans(result);
            result = ReplaceNumericIdentifiers(result);

            return result.Trim();
        }

        private string ReplaceClientName(string text, string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                return text;

            return text.Replace(clientName, "CLIENT", StringComparison.OrdinalIgnoreCase);
        }

        private string ReplaceEmails(string text)
        {
            return Regex.Replace(
                text,
                @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
                "[EMAIL]",
                RegexOptions.IgnoreCase);
        }

        private string ReplacePhones(string text)
        {
            return Regex.Replace(
                text,
                @"(?<!\w)(\+?\d[\d\s\.\-\(\)]{7,}\d)(?!\w)",
                "[TEL]",
                RegexOptions.IgnoreCase);
        }

        private string ReplaceUrls(string text)
        {
            return Regex.Replace(
                text,
                @"https?://\S+",
                "[URL]",
                RegexOptions.IgnoreCase);
        }

        private string ReplaceIbans(string text)
        {
            return Regex.Replace(
                text,
                @"\bFR\d{2}[A-Z0-9]{10,30}\b",
                "[IBAN]",
                RegexOptions.IgnoreCase);
        }

        private string ReplaceNumericIdentifiers(string text)
        {
            return Regex.Replace(
                text,
                @"\b\d{9,14}\b",
                "[IDENTIFIANT]",
                RegexOptions.IgnoreCase);
        }
    }
}