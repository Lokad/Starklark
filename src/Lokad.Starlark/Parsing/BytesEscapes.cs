using System;
using System.Collections.Generic;
using System.Text;

namespace Lokad.Starlark.Parsing;

internal static class BytesEscapes
{
    public static byte[] Unescape(string text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        var bytes = new List<byte>(text.Length);
        var buffer = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch != '\\')
            {
                buffer.Append(ch);
                continue;
            }

            FlushBuffer(bytes, buffer);
            if (i + 1 >= text.Length)
            {
                bytes.Add((byte)'\\');
                break;
            }

            var next = text[++i];
            switch (next)
            {
                case '\\': bytes.Add((byte)'\\'); break;
                case '"': bytes.Add((byte)'"'); break;
                case '\'': bytes.Add((byte)'\''); break;
                case 'n': bytes.Add((byte)'\n'); break;
                case 'r': bytes.Add((byte)'\r'); break;
                case 't': bytes.Add((byte)'\t'); break;
                case 'a': bytes.Add((byte)'\a'); break;
                case 'b': bytes.Add((byte)'\b'); break;
                case 'f': bytes.Add((byte)'\f'); break;
                case 'v': bytes.Add((byte)'\v'); break;
                case 'x':
                    bytes.Add(ReadHexByte(text, ref i, 2));
                    break;
                case 'u':
                    AppendUtf8(bytes, ReadHexScalar(text, ref i, 4));
                    break;
                case 'U':
                    AppendUtf8(bytes, ReadHexScalar(text, ref i, 8));
                    break;
                default:
                    if (next is >= '0' and <= '7')
                    {
                        bytes.Add(ReadOctalByte(text, ref i, next));
                    }
                    else
                    {
                        bytes.Add((byte)next);
                    }
                    break;
            }
        }

        FlushBuffer(bytes, buffer);
        return bytes.ToArray();
    }

    private static void FlushBuffer(List<byte> bytes, StringBuilder buffer)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        bytes.AddRange(Encoding.UTF8.GetBytes(buffer.ToString()));
        buffer.Clear();
    }

    private static byte ReadHexByte(string text, ref int index, int digits)
    {
        var value = 0;
        for (var i = 0; i < digits && index + 1 < text.Length; i++)
        {
            var digit = text[++index];
            var hex = digit switch
            {
                >= '0' and <= '9' => digit - '0',
                >= 'a' and <= 'f' => digit - 'a' + 10,
                >= 'A' and <= 'F' => digit - 'A' + 10,
                _ => 0
            };
            value = (value * 16) + hex;
        }

        return (byte)value;
    }

    private static byte ReadOctalByte(string text, ref int index, char firstDigit)
    {
        var value = firstDigit - '0';
        for (var i = 0; i < 2 && index + 1 < text.Length; i++)
        {
            var digit = text[index + 1];
            if (digit < '0' || digit > '7')
            {
                break;
            }

            index++;
            value = (value * 8) + (digit - '0');
        }

        return (byte)value;
    }

    private static int ReadHexScalar(string text, ref int index, int digits)
    {
        var value = 0;
        for (var i = 0; i < digits && index + 1 < text.Length; i++)
        {
            var digit = text[++index];
            var hex = digit switch
            {
                >= '0' and <= '9' => digit - '0',
                >= 'a' and <= 'f' => digit - 'a' + 10,
                >= 'A' and <= 'F' => digit - 'A' + 10,
                _ => 0
            };
            value = (value * 16) + hex;
        }

        return value;
    }

    private static void AppendUtf8(List<byte> bytes, int codePoint)
    {
        if (!Rune.TryCreate(codePoint, out var rune))
        {
            rune = Rune.ReplacementChar;
        }

        Span<byte> buffer = stackalloc byte[4];
        var count = rune.EncodeToUtf8(buffer);
        for (var i = 0; i < count; i++)
        {
            bytes.Add(buffer[i]);
        }
    }
}
