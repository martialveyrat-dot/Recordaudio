using System;
using System.Collections.Generic;
using System.IO;

namespace Recordaudio.Services
{
    public sealed class TranscriptRecompositionService
    {
        public string RecomposeFromChunkTexts(List<string> chunkTranscriptPaths, string outputFilePath)
        {
            var texts = new List<string>();

            foreach (var path in chunkTranscriptPaths)
            {
                if (File.Exists(path))
                {
                    texts.Add(File.ReadAllText(path).Trim());
                }
            }

            string recomposed = RecomposeRawTexts(texts);
            File.WriteAllText(outputFilePath, recomposed);
            return recomposed;
        }

        public string RecomposeRawTexts(List<string> chunkTexts)
        {
            if (chunkTexts.Count == 0)
                return "";

            string result = chunkTexts[0].Trim();

            for (int i = 1; i < chunkTexts.Count; i++)
            {
                string next = chunkTexts[i].Trim();
                result = MergeWithOverlap(result, next);
            }

            return result.Trim();
        }

        private string MergeWithOverlap(string left, string right)
        {
            int maxCharsToCheck = Math.Min(800, Math.Min(left.Length, right.Length));

            for (int len = maxCharsToCheck; len >= 40; len--)
            {
                string leftSuffix = left[^len..].Trim();
                string rightPrefix = right[..len].Trim();

                if (string.Equals(leftSuffix, rightPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return left + right[len..];
                }
            }

            return left + Environment.NewLine + Environment.NewLine + right;
        }
    }
}