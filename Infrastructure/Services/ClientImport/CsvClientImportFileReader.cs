// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads a CSV/TXT employee list. The encoding is taken from a byte-order mark (UTF-8, UTF-16 LE/BE),
/// otherwise strict UTF-8 is tried and Windows-1252 (the Excel "CSV" default on Western Windows) is the
/// fallback. The delimiter is sniffed among ; , tab and | by the most consistent count per line.
/// Quoted fields may contain delimiters, doubled quotes and line breaks; CR, LF and CRLF all end a record.
/// </summary>

using System.Text;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.ClientImport;

public class CsvClientImportFileReader : IClientImportFileReader
{
    public const string Utf8Name = "utf-8";
    public const string Utf16Name = "utf-16";
    public const string Utf16BigEndianName = "utf-16BE";
    public const string Windows1252Name = "windows-1252";

    private const int Windows1252CodePage = 1252;
    private const int SniffLines = 20;
    private const char Quote = '"';
    private const char CarriageReturn = '\r';
    private const char LineFeed = '\n';

    private static readonly string[] Extensions = [".csv", ".txt"];
    private static readonly char[] CandidateDelimiters = [';', ',', '\t', '|'];
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] Utf16LeBom = [0xFF, 0xFE];
    private static readonly byte[] Utf16BeBom = [0xFE, 0xFF];

    static CsvClientImportFileReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public bool CanRead(string fileName) =>
        Extensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

    public ClientImportSheet Read(byte[] content, string? sheetName)
    {
        var (text, encodingName) = Decode(content);
        var delimiter = SniffDelimiter(text);
        var rows = Parse(text, delimiter);

        return new ClientImportSheet
        {
            Rows = rows,
            Delimiter = delimiter.ToString(),
            Encoding = encodingName
        };
    }

    public static (string Text, string EncodingName) Decode(byte[] content)
    {
        if (StartsWith(content, Utf8Bom))
        {
            return (Encoding.UTF8.GetString(content, Utf8Bom.Length, content.Length - Utf8Bom.Length), Utf8Name);
        }

        if (StartsWith(content, Utf16LeBom))
        {
            return (Encoding.Unicode.GetString(content, Utf16LeBom.Length, content.Length - Utf16LeBom.Length), Utf16Name);
        }

        if (StartsWith(content, Utf16BeBom))
        {
            return (Encoding.BigEndianUnicode.GetString(content, Utf16BeBom.Length, content.Length - Utf16BeBom.Length), Utf16BigEndianName);
        }

        try
        {
            return (new UTF8Encoding(false, true).GetString(content), Utf8Name);
        }
        catch (DecoderFallbackException)
        {
            return (Encoding.GetEncoding(Windows1252CodePage).GetString(content), Windows1252Name);
        }
    }

    public static char SniffDelimiter(string text)
    {
        var lines = SplitLogicalLines(text).Where(l => l.Length > 0).Take(SniffLines).ToList();
        if (lines.Count == 0)
        {
            return CandidateDelimiters[0];
        }

        var best = CandidateDelimiters[0];
        var bestScore = -1;

        foreach (var candidate in CandidateDelimiters)
        {
            var counts = lines.Select(line => CountOutsideQuotes(line, candidate)).ToList();
            var commonCount = counts.GroupBy(c => c).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).First();
            var score = commonCount.Key == 0 ? 0 : commonCount.Count() * 1000 + commonCount.Key;

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    public static List<List<string>> Parse(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var index = 0;
        var maxRecords = ClientImportLimits.MaxRows + ClientImportLimits.HeaderSearchRows + 1;

        while (index < text.Length)
        {
            var character = text[index];

            if (inQuotes)
            {
                if (character == Quote)
                {
                    if (index + 1 < text.Length && text[index + 1] == Quote)
                    {
                        field.Append(Quote);
                        index += 2;
                        continue;
                    }

                    inQuotes = false;
                    index++;
                    continue;
                }

                field.Append(character);
                index++;
                continue;
            }

            if (character == Quote && field.Length == 0)
            {
                inQuotes = true;
                index++;
                continue;
            }

            if (character == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
                index++;
                continue;
            }

            if (character is CarriageReturn or LineFeed)
            {
                row.Add(field.ToString());
                field.Clear();
                AddIfNotBlank(rows, row);
                row = [];

                if (rows.Count > maxRecords)
                {
                    return rows;
                }

                index += character == CarriageReturn && index + 1 < text.Length && text[index + 1] == LineFeed ? 2 : 1;
                continue;
            }

            field.Append(character);
            index++;
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            AddIfNotBlank(rows, row);
        }

        return rows;
    }

    private static void AddIfNotBlank(List<List<string>> rows, List<string> row)
    {
        if (row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
        {
            rows.Add(row);
        }
    }

    private static IEnumerable<string> SplitLogicalLines(string text) =>
        text.Split([CarriageReturn, LineFeed], StringSplitOptions.None);

    private static int CountOutsideQuotes(string line, char delimiter)
    {
        var count = 0;
        var inQuotes = false;
        foreach (var character in line)
        {
            if (character == Quote)
            {
                inQuotes = !inQuotes;
            }
            else if (character == delimiter && !inQuotes)
            {
                count++;
            }
        }

        return count;
    }

    private static bool StartsWith(byte[] content, byte[] prefix) =>
        content.Length >= prefix.Length && content.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}
