using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Playnite.LibraryImport.Steam;

internal static class SteamVdfParser
{
    public static Dictionary<string, object> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        var tokenizer = new Tokenizer(text);
        var root = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var key = tokenizer.ReadStringToken();
            if (key == null)
            {
                break;
            }

            tokenizer.SkipWhitespace();
            if (tokenizer.PeekChar() == '{')
            {
                tokenizer.ReadChar();
                var obj = ReadObject(tokenizer);
                root[key] = obj;
            }
            else
            {
                var value = tokenizer.ReadStringToken() ?? string.Empty;
                root[key] = value;
            }
        }

        return root;
    }

    private static Dictionary<string, object> ReadObject(Tokenizer tokenizer)
    {
        var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            tokenizer.SkipWhitespace();
            var ch = tokenizer.PeekChar();
            if (ch == null)
            {
                break;
            }

            if (ch == '}')
            {
                tokenizer.ReadChar();
                break;
            }

            var key = tokenizer.ReadStringToken();
            if (key == null)
            {
                break;
            }

            tokenizer.SkipWhitespace();
            if (tokenizer.PeekChar() == '{')
            {
                tokenizer.ReadChar();
                obj[key] = ReadObject(tokenizer);
            }
            else
            {
                obj[key] = tokenizer.ReadStringToken() ?? string.Empty;
            }
        }

        return obj;
    }

    private sealed class Tokenizer
    {
        private readonly StringReader reader;
        private int? peeked;

        public Tokenizer(string text)
        {
            reader = new StringReader(text);
        }

        public char? PeekChar()
        {
            if (peeked.HasValue)
            {
                return peeked.Value < 0 ? null : (char)peeked.Value;
            }

            peeked = reader.Read();
            return peeked.Value < 0 ? null : (char)peeked.Value;
        }

        public char? ReadChar()
        {
            if (peeked.HasValue)
            {
                var v = peeked.Value;
                peeked = null;
                return v < 0 ? null : (char)v;
            }

            var c = reader.Read();
            return c < 0 ? null : (char)c;
        }

        public void SkipWhitespace()
        {
            while (true)
            {
                var ch = PeekChar();
                if (ch == null)
                {
                    return;
                }

                if (char.IsWhiteSpace(ch.Value))
                {
                    ReadChar();
                    continue;
                }

                if (ch == '/')
                {
                    ReadChar();
                    if (PeekChar() == '/')
                    {
                        while (true)
                        {
                            var c = ReadChar();
                            if (c == null || c == '\n')
                            {
                                break;
                            }
                        }
                        continue;
                    }

                    peeked = '/';
                }

                return;
            }
        }

        public string? ReadStringToken()
        {
            SkipWhitespace();
            var ch = PeekChar();
            if (ch == null)
            {
                return null;
            }

            if (ch == '{' || ch == '}')
            {
                return null;
            }

            if (ch == '"')
            {
                ReadChar();
                var sb = new StringBuilder();
                while (true)
                {
                    var c = ReadChar();
                    if (c == null)
                    {
                        break;
                    }

                    if (c == '"')
                    {
                        break;
                    }

                    if (c == '\\')
                    {
                        var next = ReadChar();
                        if (next != null)
                        {
                            sb.Append(next.Value);
                        }
                        continue;
                    }

                    sb.Append(c.Value);
                }

                return sb.ToString();
            }

            var token = new StringBuilder();
            while (true)
            {
                var c = PeekChar();
                if (c == null || char.IsWhiteSpace(c.Value) || c == '{' || c == '}')
                {
                    break;
                }

                token.Append(ReadChar());
            }

            return token.Length == 0 ? null : token.ToString();
        }
    }
}

