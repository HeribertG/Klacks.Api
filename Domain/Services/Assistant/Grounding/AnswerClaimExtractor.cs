// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, locale-tolerant extraction of hard claims (UUIDs, dates, numbers) from a sanitized
/// assistant answer. Numbers are parsed structurally with a double-reading strategy — never
/// NumberStyles.Any — so "1.234" yields both 1234 and 1.234 and coverage of either reading
/// counts. Non-ASCII decimal digits are normalized first, so CJK fullwidth and Arabic-Indic
/// digits are extracted like ASCII ones. Uncertain tokens yield no claim (silence is safe).
/// </summary>
/// <param name="sanitizedText">Answer text after AnswerGroundingResponseSanitizer.</param>
/// <param name="language">ISO language of the turn, used only for month-name date parsing.</param>

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Assistant.Grounding;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public static class AnswerClaimExtractor
{
    private const int MinSignificantInteger = 100;
    private const int IgnoredYearFrom = 1990;
    private const int IgnoredYearTo = 2035;

    private static readonly Regex UuidRegex = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled);

    private static readonly Regex IsoDateRegex = new(@"\b(\d{4})-(\d{2})-(\d{2})\b", RegexOptions.Compiled);
    private static readonly Regex DottedDateRegex = new(@"\b(\d{1,2})\.(\d{1,2})\.(\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex SlashDateRegex = new(@"\b(\d{1,2})/(\d{1,2})/(\d{4})\b", RegexOptions.Compiled);
    private static readonly Regex CjkDateRegex = new(@"(\d{4})年(\d{1,2})月(\d{1,2})日", RegexOptions.Compiled);

    private static readonly Regex NumberRegex = new(
        @"\d[\d.,'’   ]*\d|\d",
        RegexOptions.Compiled);

    private static readonly char[] GroupingOnlySeparators = ['\'', '’', ' ', ' ', ' '];

    public static IReadOnlyList<AnswerClaim> Extract(string sanitizedText, string? language)
    {
        if (string.IsNullOrWhiteSpace(sanitizedText))
        {
            return Array.Empty<AnswerClaim>();
        }

        var claims = new List<AnswerClaim>();
        var text = NormalizeDigits(sanitizedText);

        text = ExtractUuids(text, claims);
        text = ExtractDates(text, language, claims);
        ExtractNumbers(text, claims);

        return claims;
    }

    internal static string NormalizeDigits(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormKC);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (char.IsDigit(c) && c > '9')
            {
                sb.Append((char)('0' + CharUnicodeInfo.GetDecimalDigitValue(c)));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string ExtractUuids(string text, List<AnswerClaim> claims)
    {
        return UuidRegex.Replace(text, match =>
        {
            claims.Add(new AnswerClaim(
                AnswerClaimKind.Uuid,
                match.Value,
                [match.Value.ToLowerInvariant()]));
            return new string(' ', match.Value.Length);
        });
    }

    private static string ExtractDates(string text, string? language, List<AnswerClaim> claims)
    {
        text = ReplaceDates(text, IsoDateRegex, claims, m =>
            TryDate(m.Groups[1], m.Groups[2], m.Groups[3], out var d) ? [d] : []);

        text = ReplaceDates(text, CjkDateRegex, claims, m =>
            TryDate(m.Groups[1], m.Groups[2], m.Groups[3], out var d) ? [d] : []);

        text = ReplaceDates(text, DottedDateRegex, claims, m =>
            TryDate(m.Groups[3], m.Groups[2], m.Groups[1], out var d) ? [d] : []);

        text = ReplaceDates(text, SlashDateRegex, claims, m =>
        {
            var readings = new List<string>();
            if (TryDate(m.Groups[3], m.Groups[2], m.Groups[1], out var dayFirst))
            {
                readings.Add(dayFirst);
            }

            if (TryDate(m.Groups[3], m.Groups[1], m.Groups[2], out var monthFirst) && !readings.Contains(monthFirst))
            {
                readings.Add(monthFirst);
            }

            return readings;
        });

        var monthNameRegexes = BuildMonthNameRegexes(language);
        foreach (var (regex, buildReadings) in monthNameRegexes)
        {
            text = ReplaceDates(text, regex, claims, buildReadings);
        }

        return text;
    }

    private static string ReplaceDates(
        string text,
        Regex regex,
        List<AnswerClaim> claims,
        Func<Match, List<string>> buildReadings)
    {
        return regex.Replace(text, match =>
        {
            var readings = buildReadings(match);
            if (readings.Count == 0)
            {
                return match.Value;
            }

            claims.Add(new AnswerClaim(AnswerClaimKind.Date, match.Value, readings));
            return new string(' ', match.Value.Length);
        });
    }

    private static bool TryDate(Capture year, Capture month, Capture day, out string isoDate)
    {
        isoDate = string.Empty;
        if (!int.TryParse(year.Value, out var y) || !int.TryParse(month.Value, out var m) || !int.TryParse(day.Value, out var d))
        {
            return false;
        }

        if (m is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(Math.Clamp(y, 1, 9999), Math.Clamp(m, 1, 12)))
        {
            return false;
        }

        isoDate = new DateOnly(y, m, d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private static List<(Regex Regex, Func<Match, List<string>> BuildReadings)> BuildMonthNameRegexes(string? language)
    {
        var result = new List<(Regex, Func<Match, List<string>>)>();
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(language) ? "en" : language!);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }

        var monthByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var month = 1; month <= 12; month++)
        {
            AddMonthName(monthByName, culture.DateTimeFormat.MonthNames[month - 1], month);
            AddMonthName(monthByName, culture.DateTimeFormat.AbbreviatedMonthNames[month - 1], month);
        }

        if (monthByName.Count == 0)
        {
            return result;
        }

        var alternation = string.Join("|", monthByName.Keys
            .OrderByDescending(n => n.Length)
            .Select(Regex.Escape));

        var dayFirst = new Regex(
            $@"\b(\d{{1,2}})\.?\s+({alternation})\.?\s+(\d{{4}})\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        result.Add((dayFirst, m => MonthNameReading(monthByName, m.Groups[3], m.Groups[2], m.Groups[1])));

        var monthFirst = new Regex(
            $@"\b({alternation})\.?\s+(\d{{1,2}})(?:st|nd|rd|th)?,?\s+(\d{{4}})\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        result.Add((monthFirst, m => MonthNameReading(monthByName, m.Groups[3], m.Groups[1], m.Groups[2])));

        return result;
    }

    private static void AddMonthName(Dictionary<string, int> map, string name, int month)
    {
        var trimmed = name.Trim().TrimEnd('.');
        if (trimmed.Length >= 3)
        {
            map[trimmed] = month;
        }
    }

    private static List<string> MonthNameReading(
        Dictionary<string, int> monthByName, Capture year, Capture monthName, Capture day)
    {
        if (!monthByName.TryGetValue(monthName.Value.TrimEnd('.'), out var month))
        {
            return [];
        }

        if (!int.TryParse(year.Value, out var y) || !int.TryParse(day.Value, out var d))
        {
            return [];
        }

        if (d < 1 || d > DateTime.DaysInMonth(y, month))
        {
            return [];
        }

        return [new DateOnly(y, month, d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)];
    }

    private static void ExtractNumbers(string text, List<AnswerClaim> claims)
    {
        foreach (Match match in NumberRegex.Matches(text))
        {
            var isPercent = IsFollowedByPercent(text, match.Index + match.Length);
            foreach (var token in SplitInvalidSpaceGroups(match.Value))
            {
                var readings = ParseNumberReadings(token);
                if (readings.Count == 0)
                {
                    continue;
                }

                if (isPercent || readings.All(IsIgnorableReading))
                {
                    continue;
                }

                claims.Add(new AnswerClaim(AnswerClaimKind.Number, token, readings));
            }
        }
    }

    private static bool IsFollowedByPercent(string text, int index)
    {
        while (index < text.Length && text[index] == ' ')
        {
            index++;
        }

        return index < text.Length && text[index] == '%';
    }

    internal static IEnumerable<string> SplitInvalidSpaceGroups(string token)
    {
        var spaceSeparators = new[] { ' ', ' ', ' ' };
        if (token.IndexOfAny(spaceSeparators) < 0)
        {
            yield return token.Trim();
            yield break;
        }

        var groups = token.Split(spaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        var lastGroup = groups[^1];
        var decimalIndex = lastGroup.IndexOfAny(['.', ',']);
        var lastDigits = decimalIndex >= 0 ? lastGroup[..decimalIndex] : lastGroup;
        var decimalTailValid = decimalIndex < 0
            || (lastGroup.Length > decimalIndex + 1 && lastGroup[(decimalIndex + 1)..].All(char.IsDigit));
        var validGrouping = groups.Length > 1
            && groups[0].Length is >= 1 and <= 3
            && groups[0].All(char.IsDigit)
            && groups[1..^1].All(g => g.Length == 3 && g.All(char.IsDigit))
            && lastDigits.Length == 3
            && lastDigits.All(char.IsDigit)
            && decimalTailValid;

        if (validGrouping)
        {
            yield return string.Concat(groups);
        }
        else
        {
            foreach (var group in groups)
            {
                yield return group.Trim();
            }
        }
    }

    internal static List<string> ParseNumberReadings(string token)
    {
        var readings = new List<string>();
        token = token.Trim().Trim(GroupingOnlySeparators).Trim('.', ',');
        if (token.Length == 0)
        {
            return readings;
        }

        foreach (var separator in GroupingOnlySeparators)
        {
            if (!token.Contains(separator))
            {
                continue;
            }

            var groups = token.Split(separator);
            var lastGroup = groups[^1];
            var decimalTail = string.Empty;
            var decimalIndex = lastGroup.IndexOfAny(['.', ',']);
            if (decimalIndex >= 0)
            {
                decimalTail = lastGroup[decimalIndex..];
                lastGroup = lastGroup[..decimalIndex];
                if (decimalTail.Length < 2 || !decimalTail[1..].All(char.IsDigit))
                {
                    return readings;
                }
            }

            var validGrouping = groups.Length > 1
                && groups[0].Length is >= 1 and <= 3
                && groups[0].All(char.IsDigit)
                && groups[1..^1].All(g => g.Length == 3 && g.All(char.IsDigit))
                && lastGroup.Length == 3
                && lastGroup.All(char.IsDigit);

            if (!validGrouping)
            {
                return readings;
            }

            token = string.Concat(groups[..^1]) + lastGroup + decimalTail;
        }

        var dots = token.Count(c => c == '.');
        var commas = token.Count(c => c == ',');

        if (dots == 0 && commas == 0)
        {
            if (decimal.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var plain))
            {
                readings.Add(Normalize(plain));
            }

            return readings;
        }

        if (dots > 0 && commas > 0)
        {
            var lastDot = token.LastIndexOf('.');
            var lastComma = token.LastIndexOf(',');
            var decimalSeparator = lastDot > lastComma ? '.' : ',';
            AddReading(readings, StripToDecimal(token, decimalSeparator));
            return readings;
        }

        var single = dots > 0 ? '.' : ',';
        var parts = token.Split(single);
        var tail = parts[^1];

        if (parts.Length > 2 || tail.Length == 0 || parts[0].Length == 0)
        {
            if (parts.Skip(1).All(p => p.Length == 3) && parts[0].Length is >= 1 and <= 3)
            {
                AddReading(readings, string.Concat(parts));
            }

            return readings;
        }

        if (tail.Length is 1 or 2)
        {
            AddReading(readings, parts[0] + "." + tail);
            return readings;
        }

        if (tail.Length == 3 && parts[0].Length is >= 1 and <= 3)
        {
            AddReading(readings, string.Concat(parts));
            AddReading(readings, parts[0] + "." + tail);
            return readings;
        }

        AddReading(readings, parts[0] + "." + tail);
        return readings;
    }

    private static string StripToDecimal(string token, char decimalSeparator)
    {
        var lastIndex = token.LastIndexOf(decimalSeparator);
        var integerPart = new string(token[..lastIndex].Where(char.IsDigit).ToArray());
        var fraction = token[(lastIndex + 1)..];
        return fraction.All(char.IsDigit) && fraction.Length > 0 && integerPart.Length > 0
            ? integerPart + "." + fraction
            : string.Empty;
    }

    private static void AddReading(List<string> readings, string candidate)
    {
        if (candidate.Length == 0)
        {
            return;
        }

        if (decimal.TryParse(candidate, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            var normalized = Normalize(value);
            if (!readings.Contains(normalized))
            {
                readings.Add(normalized);
            }
        }
    }

    private static bool IsIgnorableReading(string reading)
    {
        if (!decimal.TryParse(reading, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return true;
        }

        if (value != decimal.Truncate(value))
        {
            return false;
        }

        return value is < MinSignificantInteger or (>= IgnoredYearFrom and <= IgnoredYearTo);
    }

    internal static string Normalize(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
}
