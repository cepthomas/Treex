using System;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;


namespace Ephemera.NBagOfTricks
{
    public class IniSyntaxException(string message, int lineNum) : Exception(message)
    {
        public int LineNum { get; init; } = lineNum;
    }


    public class IniReader
    {
        public Dictionary<string, Dictionary<string, string>> Contents { get; } = [];

        public IniReader(string fn)
        {
            string currentSection = "";
            Dictionary<string, string> currentValues = [];
            int lineNum = 0;

            foreach (var inline in File.ReadAllLines(fn))
            {
                lineNum++;

                // Strip comments.
                var cmt = inline.IndexOf(';');
                var line = cmt >= 0 ? inline[0..cmt] : inline;

                line = line.Trim();

                // Ignore empty lines.
                if (line.Length == 0)
                {
                    continue;
                }

                // Section?
                if (line[0] == '[')
                {
                    if (line[^1 ] == ']')
                    {
                        // New section. Is it the first?
                        if (currentSection != "")
                        {
                            if (currentValues.Count > 0)
                            {
                                // Save last.
                                Contents[currentSection] = currentValues;
                                currentValues = new();
                            }
                            else
                            {
                                throw new IniSyntaxException($"Section {currentSection} has no elements", lineNum);
                            }
                        }

                        var sectionName = line[1..^1];
                        if (Contents.ContainsKey(sectionName))
                        {
                            throw new IniSyntaxException($"Duplicate section: {inline}", lineNum);
                        }

                        currentSection = sectionName;
                        currentValues.Clear();
                    }
                    else
                    {
                        throw new IniSyntaxException($"Invalid section: {inline}", lineNum);
                    }
                    continue;
                }

                // Just a value.
                if (currentSection == "")
                {
                    throw new IniSyntaxException($"Global values not supported: {inline}", lineNum);
                }

                var parts = line.SplitByToken("=");
                if (parts.Count !=2)
                {
                    throw new IniSyntaxException($"Invalid value: {inline}", lineNum);
                }

                // Remove any quotes.
                var lhs = parts[0].Replace("\"", "");
                var rhs = parts[1].Replace("\"", "");

                if (currentValues.ContainsKey(lhs))
                {
                    throw new IniSyntaxException($"Duplicate key: {inline}", lineNum);
                }

                currentValues.Add(lhs, rhs); // TODO duplicate keys?
            }

            // Anything left?
            if (currentValues.Count > 0)
            {
                // Save last.
                Contents[currentSection] = currentValues;
                currentValues = new();
            }
        }
    }
}
