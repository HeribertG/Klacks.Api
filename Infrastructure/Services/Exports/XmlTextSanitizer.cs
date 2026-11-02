// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Removes characters that are not allowed in XML 1.0 documents (control characters except tab, LF and CR,
/// unpaired surrogates, U+FFFE and U+FFFF) so that free text never makes XmlWriter throw.
/// </summary>
using System.Text;

namespace Klacks.Api.Infrastructure.Services.Exports;

public static class XmlTextSanitizer
{
    public static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder? sb = null;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var keep = true;
            var pair = false;

            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    pair = true;
                }
                else
                {
                    keep = false;
                }
            }
            else if (char.IsLowSurrogate(c))
            {
                keep = false;
            }
            else
            {
                keep = c == '\t' || c == '\n' || c == '\r'
                    || (c >= 0x20 && c <= 0xD7FF)
                    || (c >= 0xE000 && c <= 0xFFFD);
            }

            if (keep)
            {
                sb?.Append(c);
                if (pair)
                {
                    i++;
                    sb?.Append(value[i]);
                }
            }
            else if (sb == null)
            {
                sb = new StringBuilder(value.Length);
                sb.Append(value, 0, i);
            }
        }

        return sb?.ToString() ?? value;
    }
}
