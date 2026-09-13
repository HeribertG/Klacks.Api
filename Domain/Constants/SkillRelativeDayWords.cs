// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The relative day words a user may send instead of a concrete date - today, tomorrow and yesterday -
/// in all 25 supported UI languages and in their native scripts. A lookup is scoped to the user's own
/// language plus English, because the 25 languages share homographs that mean something else in each
/// other: French "hier" (yesterday) is German for "here", German and Dutch "morgen" (tomorrow) is also
/// the noun "morning", and the Spanish word for tomorrow is the word for morning as well. Resolving
/// against the union of all languages therefore turns a German user's stray "hier" into yesterday.
/// English is always allowed on top of the user's language because the model frequently normalises a
/// relative day to English before it fills the parameter. A null or unrecognised language - the batch
/// and scheduler paths, which have no user - falls back to the full union, the behaviour every caller
/// had before.
///
/// "zh-CN" and "zh-TW" are kept whole as keys the way every other language-keyed table in Klacks keeps
/// them, so a bare "zh" matches neither and resolves against the union instead.
///
/// The three flat word lists stay the authoritative inventory and are what the coverage guard counts;
/// the per-language table lists the same words again under their languages, and a guard asserts that
/// the two views hold exactly the same words so neither can drift. Shared by the dispatch-time
/// parameter gate (<c>SkillParameterTypeValidator</c>) and the skill-level parsing
/// (<c>SkillDateParser</c>), which must be given the same language so a word the gate accepts is never
/// rejected by the parser afterwards, and the other way round.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class SkillRelativeDayWords
{
    public const int TodayOffset = 0;
    public const int TomorrowOffset = 1;
    public const int YesterdayOffset = -1;

    private const string EnglishLanguage = "en";

    private const char TypographicApostrophe = '’';
    private const char PlainApostrophe = '\'';

    public static readonly string[] TodayWords =
    {
        "today", "now",
        "heute", "jetzt", "sofort", "ab sofort", "ab heute",
        "aujourd'hui",
        "oggi",
        "hoy",
        "hoje",
        "vandaag",
        "dzisiaj", "dziś", "dzis",
        "dnes",
        "i dag", "idag",
        "tänään", "tanaan",
        "azi", "astăzi", "astazi",
        "σήμερα",
        "היום",
        "اليوم",
        "今日", "きょう", "本日",
        "오늘",
        "วันนี้",
        "hôm nay", "hom nay",
        "今天",
        "hari ini"
    };

    public static readonly string[] TomorrowWords =
    {
        "tomorrow",
        "morgen",
        "demain",
        "domani",
        "mañana", "manana",
        "amanhã", "amanha",
        "jutro",
        "zítra", "zitra",
        "i morgen", "imorgen", "i morgon", "imorgon",
        "huomenna",
        "mâine", "maine",
        "αύριο",
        "מחר",
        "غدا", "غداً", "الغد",
        "明日", "あした", "あす",
        "내일",
        "พรุ่งนี้",
        "ngày mai", "ngay mai",
        "明天",
        "besok", "esok"
    };

    public static readonly string[] YesterdayWords =
    {
        "yesterday",
        "gestern",
        "hier",
        "ieri",
        "ayer",
        "ontem",
        "gisteren",
        "wczoraj",
        "včera", "vcera",
        "i går", "igår", "i gar", "igar",
        "eilen",
        "χθες", "εχθές",
        "אתמול",
        "أمس", "الأمس",
        "昨日", "きのう",
        "어제",
        "เมื่อวาน", "เมื่อวานนี้",
        "hôm qua", "hom qua",
        "昨天",
        "kemarin", "semalam"
    };

    private static readonly IReadOnlyDictionary<string, string[]> TodayWordsByLanguage =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = ["today", "now"],
            ["de"] = ["heute", "jetzt", "sofort", "ab sofort", "ab heute"],
            ["fr"] = ["aujourd'hui"],
            ["it"] = ["oggi"],
            ["es"] = ["hoy"],
            ["pt"] = ["hoje"],
            ["nl"] = ["vandaag"],
            ["pl"] = ["dzisiaj", "dziś", "dzis"],
            ["cs"] = ["dnes"],
            ["da"] = ["i dag", "idag"],
            ["nb"] = ["i dag", "idag"],
            ["sv"] = ["i dag", "idag"],
            ["fi"] = ["tänään", "tanaan"],
            ["ro"] = ["azi", "astăzi", "astazi"],
            ["el"] = ["σήμερα"],
            ["he"] = ["היום"],
            ["ar"] = ["اليوم"],
            ["ja"] = ["今日", "きょう", "本日"],
            ["ko"] = ["오늘"],
            ["th"] = ["วันนี้"],
            ["vi"] = ["hôm nay", "hom nay"],
            ["zh-CN"] = ["今天"],
            ["zh-TW"] = ["今天"],
            ["id"] = ["hari ini"],
            ["ms"] = ["hari ini"]
        };

    private static readonly IReadOnlyDictionary<string, string[]> TomorrowWordsByLanguage =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = ["tomorrow"],
            ["de"] = ["morgen"],
            ["fr"] = ["demain"],
            ["it"] = ["domani"],
            ["es"] = ["mañana", "manana"],
            ["pt"] = ["amanhã", "amanha"],
            ["nl"] = ["morgen"],
            ["pl"] = ["jutro"],
            ["cs"] = ["zítra", "zitra"],
            ["da"] = ["i morgen", "imorgen"],
            ["nb"] = ["i morgen", "imorgen"],
            ["sv"] = ["i morgon", "imorgon"],
            ["fi"] = ["huomenna"],
            ["ro"] = ["mâine", "maine"],
            ["el"] = ["αύριο"],
            ["he"] = ["מחר"],
            ["ar"] = ["غدا", "غداً", "الغد"],
            ["ja"] = ["明日", "あした", "あす"],
            ["ko"] = ["내일"],
            ["th"] = ["พรุ่งนี้"],
            ["vi"] = ["ngày mai", "ngay mai"],
            ["zh-CN"] = ["明天"],
            ["zh-TW"] = ["明天"],
            ["id"] = ["besok"],
            ["ms"] = ["esok"]
        };

    private static readonly IReadOnlyDictionary<string, string[]> YesterdayWordsByLanguage =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = ["yesterday"],
            ["de"] = ["gestern"],
            ["fr"] = ["hier"],
            ["it"] = ["ieri"],
            ["es"] = ["ayer"],
            ["pt"] = ["ontem"],
            ["nl"] = ["gisteren"],
            ["pl"] = ["wczoraj"],
            ["cs"] = ["včera", "vcera"],
            ["da"] = ["i går", "igår", "i gar", "igar"],
            ["nb"] = ["i går", "igår", "i gar", "igar"],
            ["sv"] = ["i går", "igår", "i gar", "igar"],
            ["fi"] = ["eilen"],
            ["ro"] = ["ieri"],
            ["el"] = ["χθες", "εχθές"],
            ["he"] = ["אתמול"],
            ["ar"] = ["أمس", "الأمس"],
            ["ja"] = ["昨日", "きのう"],
            ["ko"] = ["어제"],
            ["th"] = ["เมื่อวาน", "เมื่อวานนี้"],
            ["vi"] = ["hôm qua", "hom qua"],
            ["zh-CN"] = ["昨天"],
            ["zh-TW"] = ["昨天"],
            ["id"] = ["kemarin"],
            ["ms"] = ["semalam"]
        };

    private static readonly IReadOnlyDictionary<string, int> OffsetByWord = BuildOffsetByWord();

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> OffsetByWordByLanguage =
        BuildOffsetByWordByLanguage();

    /// <summary>
    /// Every language that ships its own relative day words, keyed as a lookup keys them. Built from
    /// the union of the three tables, so a language listed in only two of them still gets its own
    /// entry and the guard can report the missing third instead of the language silently widening to
    /// the union of all 25 languages.
    /// </summary>
    public static IReadOnlyCollection<string> LanguagesWithOwnWords { get; } =
        OffsetByWordByLanguage.Keys.ToArray();

    /// <param name="value">Raw parameter value as the user or the model wrote it</param>
    /// <param name="language">UI language of the calling user; null or a language Klacks ships no words
    /// for widens the lookup to every language, which is what the batch and scheduler paths need.</param>
    public static bool IsRelativeDayWord(string? value, string? language) =>
        TryResolveDayOffset(value, language, out _);

    /// <param name="value">Raw parameter value as the user or the model wrote it</param>
    /// <param name="language">UI language of the calling user; the word is looked up in that language
    /// and in English only. Null or an unknown language falls back to all languages together.</param>
    /// <param name="dayOffset">Days to add to the company's current calendar day: 0, +1 or -1</param>
    public static bool TryResolveDayOffset(string? value, string? language, out int dayOffset)
    {
        dayOffset = TodayOffset;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return WordsFor(language).TryGetValue(Normalize(value), out dayOffset);
    }

    /// <summary>
    /// Every relative day word a user of that language may write: the language's own words plus the
    /// English ones. A null or unknown language yields the words of all languages together.
    /// </summary>
    /// <param name="language">UI language tag, e.g. "de", "de-CH" or "zh-CN"</param>
    public static IReadOnlyCollection<string> WordsForLanguage(string? language) =>
        WordsFor(language).Keys.ToArray();

    private static IReadOnlyDictionary<string, int> WordsFor(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return OffsetByWord;
        }

        if (OffsetByWordByLanguage.TryGetValue(language.Trim(), out var exact))
        {
            return exact;
        }

        var baseLanguage = LanguageTag.BaseLanguage(language);
        return baseLanguage is not null && OffsetByWordByLanguage.TryGetValue(baseLanguage, out var byBase)
            ? byBase
            : OffsetByWord;
    }

    /// <summary>
    /// The exact spelling a word is looked up under: trimmed and with the typographic apostrophe
    /// folded onto the plain one, so "aujourd’hui" and "aujourd'hui" are the same word. Public so a
    /// caller comparing the raw word lists (e.g. the coverage guard) folds them the same way the
    /// runtime map does instead of comparing spellings the lookup never sees.
    /// </summary>
    /// <param name="value">Relative day word as written in the lists or by the user</param>
    public static string Normalize(string value) =>
        value.Trim().Replace(TypographicApostrophe, PlainApostrophe);

    private static IReadOnlyDictionary<string, int> BuildOffsetByWord()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Add(map, TodayWords, TodayOffset);
        Add(map, TomorrowWords, TomorrowOffset);
        Add(map, YesterdayWords, YesterdayOffset);
        return map;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> BuildOffsetByWordByLanguage()
    {
        var byLanguage = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        var languages = TodayWordsByLanguage.Keys
            .Concat(TomorrowWordsByLanguage.Keys)
            .Concat(YesterdayWordsByLanguage.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var language in languages)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            AddLanguage(map, TodayWordsByLanguage, EnglishLanguage, TodayOffset);
            AddLanguage(map, TomorrowWordsByLanguage, EnglishLanguage, TomorrowOffset);
            AddLanguage(map, YesterdayWordsByLanguage, EnglishLanguage, YesterdayOffset);
            AddLanguage(map, TodayWordsByLanguage, language, TodayOffset);
            AddLanguage(map, TomorrowWordsByLanguage, language, TomorrowOffset);
            AddLanguage(map, YesterdayWordsByLanguage, language, YesterdayOffset);
            byLanguage[language] = map;
        }

        return byLanguage;
    }

    private static void AddLanguage(
        IDictionary<string, int> map,
        IReadOnlyDictionary<string, string[]> source,
        string language,
        int offset)
    {
        if (source.TryGetValue(language, out var words))
        {
            Add(map, words, offset);
        }
    }

    private static void Add(IDictionary<string, int> map, IEnumerable<string> words, int offset)
    {
        foreach (var word in words)
        {
            map[Normalize(word)] = offset;
        }
    }
}
