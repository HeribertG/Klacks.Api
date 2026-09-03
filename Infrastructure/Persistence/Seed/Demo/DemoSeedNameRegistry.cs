// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Hands out collision-free, language-specific shift names and abbreviations for the demo seed.
/// A single instance must be shared by everything that names demo shifts in one run, otherwise
/// two categories can hand out the same name.
/// </summary>
/// <param name="language">Two-letter language code selecting the translated base name</param>

namespace Klacks.Api.Data.Seed.Demo;

public class DemoSeedNameRegistry
{
    private static readonly Dictionary<string, Dictionary<string, string>> NameTranslations = new()
    {
        ["Morgenschicht"] = new() { ["ar"] = "وردية صباحية", ["he"] = "משמרת בוקר", ["ja"] = "朝番" },
        ["Tagschicht"] = new() { ["ar"] = "وردية نهارية", ["he"] = "משמרת יום", ["ja"] = "日勤" },
        ["Nachtdienst Mo-Fr"] = new() { ["ar"] = "مناوبة ليلية الإثنين-الجمعة", ["he"] = "תורנות לילה ב׳-ו׳", ["ja"] = "夜勤 月-金" },
        ["Nachtdienst Sa-So"] = new() { ["ar"] = "مناوبة ليلية السبت-الأحد", ["he"] = "תורנות לילה ש׳-א׳", ["ja"] = "夜勤 土-日" },
        ["24h-Schichtdienst"] = new() { ["ar"] = "دوام 24 ساعة", ["he"] = "משמרת 24 שעות", ["ja"] = "24時間勤務" },
        ["Frühschicht-Teil"] = new() { ["ar"] = "جزء الوردية الصباحية", ["he"] = "חלק משמרת בוקר", ["ja"] = "早番部分" },
        ["Spätschicht-Teil"] = new() { ["ar"] = "جزء الوردية المسائية", ["he"] = "חלק משמרת ערב", ["ja"] = "遅番部分" },
        ["Nachtschicht-Teil"] = new() { ["ar"] = "جزء الوردية الليلية", ["he"] = "חלק משמרת לילה", ["ja"] = "夜勤部分" },
        ["Nachtschicht-Teilung"] = new() { ["ar"] = "تقسيم الوردية الليلية", ["he"] = "פיצול משמרת לילה", ["ja"] = "夜勤分割" },
        ["Vor-Mitternacht-Teil"] = new() { ["ar"] = "جزء ما قبل منتصف الليل", ["he"] = "חלק לפני חצות", ["ja"] = "深夜前部分" },
        ["Nach-Mitternacht-Teil"] = new() { ["ar"] = "جزء ما بعد منتصف الليل", ["he"] = "חלק אחרי חצות", ["ja"] = "深夜後部分" },
    };

    private static readonly Dictionary<string, Dictionary<string, string>> AbbreviationTranslations = new()
    {
        ["MOR"] = new() { ["ar"] = "صبح", ["he"] = "בקר", ["ja"] = "朝" },
        ["TAG"] = new() { ["ar"] = "نهر", ["he"] = "יום", ["ja"] = "日" },
        ["NMF"] = new() { ["ar"] = "لنج", ["he"] = "לבו", ["ja"] = "夜月金" },
        ["NSS"] = new() { ["ar"] = "لسح", ["he"] = "לשא", ["ja"] = "夜土日" },
        ["24H"] = new() { ["ar"] = "24س", ["he"] = "24ש", ["ja"] = "24時" },
        ["F"] = new() { ["ar"] = "ص", ["he"] = "ב", ["ja"] = "早" },
        ["S"] = new() { ["ar"] = "م", ["he"] = "ע", ["ja"] = "遅" },
        ["N"] = new() { ["ar"] = "ل", ["he"] = "ל", ["ja"] = "夜" },
        ["NCT"] = new() { ["ar"] = "تل", ["he"] = "פל", ["ja"] = "夜分" },
        ["VM"] = new() { ["ar"] = "قم", ["he"] = "לח", ["ja"] = "前" },
        ["NM"] = new() { ["ar"] = "بم", ["he"] = "אח", ["ja"] = "後" },
    };

    private readonly HashSet<string> _usedNames = [];
    private readonly HashSet<string> _usedAbbreviations = [];
    private readonly string _language;

    public DemoSeedNameRegistry(string language)
    {
        _language = language;
    }

    public string UniqueName(string baseName, int counter)
    {
        if (NameTranslations.TryGetValue(baseName, out var translations) && translations.TryGetValue(_language, out var translated))
        {
            baseName = translated;
        }

        var name = counter == 1 ? baseName : $"{baseName} {counter}";
        while (_usedNames.Contains(name))
        {
            counter++;
            name = $"{baseName} {counter}";
        }

        _usedNames.Add(name);
        return name;
    }

    public string UniqueAbbreviation(string baseAbbreviation, int counter)
    {
        if (AbbreviationTranslations.TryGetValue(baseAbbreviation, out var translations) && translations.TryGetValue(_language, out var translated))
        {
            baseAbbreviation = translated;
        }

        var abbreviation = counter == 1 ? baseAbbreviation : $"{baseAbbreviation}{counter}";
        while (_usedAbbreviations.Contains(abbreviation))
        {
            counter++;
            abbreviation = $"{baseAbbreviation}{counter}";
        }

        _usedAbbreviations.Add(abbreviation);
        return abbreviation;
    }
}
