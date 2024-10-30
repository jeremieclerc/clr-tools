using System;
using System.Collections.Generic;
using System.Text;

namespace JsonParserLib
{
    public static class JsonParser
    {
        public static Dictionary<string, string> ParseJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            var jsonDict = new Dictionary<string, string>();
            json = json.Trim();

            // Remove the curly braces
            if (json.StartsWith("{") && json.EndsWith("}"))
            {
                json = json.Substring(1, json.Length - 2);
            }
            else
            {
                throw new FormatException("JSON should start with '{' and end with '}'.");
            }

            int position = 0;
            int length = json.Length;

            while (position < length)
            {
                SkipWhitespace(json, ref position);

                // Read key
                string key = ParseString(json, ref position);

                SkipWhitespace(json, ref position);

                // Expect colon
                if (position >= length || json[position] != ':')
                    throw new FormatException($"Expected ':' after key at position {position}.");
                position++;

                SkipWhitespace(json, ref position);

                // Read value
                string value;
                if (position < length && json[position] == '"')
                {
                    value = ParseString(json, ref position);
                }
                else
                {
                    // Parse number or other unquoted value
                    int start = position;
                    while (position < length && (char.IsDigit(json[position]) || json[position] == '-' || json[position] == '.'))
                    {
                        position++;
                    }
                    if (start == position)
                        throw new FormatException($"Unexpected character at position {position}.");

                    value = json.Substring(start, position - start);
                }

                // Add to dictionary
                jsonDict[key] = value;

                SkipWhitespace(json, ref position);

                // If there's a comma, skip it
                if (position < length && json[position] == ',')
                {
                    position++;
                    continue;
                }
                else if (position < length)
                {
                    throw new FormatException($"Expected ',' or end of string at position {position}.");
                }
            }

            return jsonDict;
        }

        private static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length && char.IsWhiteSpace(s[pos]))
                pos++;
        }

        private static string ParseString(string s, ref int position)
        {
            if (position >= s.Length || s[position] != '"')
                throw new FormatException($"Expected '\"' at position {position}.");

            position++; // Skip opening quote
            var sb = new StringBuilder();
            while (position < s.Length)
            {
                char c = s[position];
                if (c == '\\')
                {
                    position++;
                    if (position >= s.Length)
                        throw new FormatException($"Unexpected end after escape character at position {position}.");

                    char escapedChar = s[position];
                    switch (escapedChar)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append(escapedChar);
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            // Unicode escape sequence
                            if (position + 4 >= s.Length)
                                throw new FormatException($"Incomplete unicode escape sequence at position {position}.");

                            string hex = s.Substring(position + 1, 4);
                            if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                                throw new FormatException($"Invalid unicode escape sequence '\\u{hex}' at position {position}.");

                            sb.Append((char)codePoint);
                            position += 4;
                            break;
                        default:
                            throw new FormatException($"Invalid escape character '\\{escapedChar}' at position {position}.");
                    }
                }
                else if (c == '"')
                {
                    position++; // Skip closing quote
                    return sb.ToString();
                }
                else
                {
                    sb.Append(c);
                }
                position++;
            }

            throw new FormatException($"Unexpected end of string while parsing string starting at position {position}.");
        }
    }
}
