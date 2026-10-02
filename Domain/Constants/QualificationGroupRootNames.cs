// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Name of the top-level group partition_clients_by_qualification creates (or reuses) when no root group is
/// given, per installation language. It is a group name stored in the database, not a sentence, so it lives in
/// one code table for all 25 shipped languages instead of the language packs: the group must get the same name
/// whether or not a pack is installed, and a re-run must find it again by that name. A regional tag is tried in
/// full first (zh-CN and zh-TW differ) and then reduced to its base language (de-CH reaches German); only an
/// unknown language falls back to English.
/// </summary>
/// <param name="language">The installation's DEFAULT_LANGUAGE, possibly regional; null or empty yields English</param>

namespace Klacks.Api.Domain.Constants;

public static class QualificationGroupRootNames
{
    public const string FallbackLanguage = "en";

    private const char RegionSeparator = '-';

    public static readonly IReadOnlyDictionary<string, string> ByLanguage =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = "Qualifikationen",
            ["en"] = "Qualifications",
            ["fr"] = "Qualifications",
            ["it"] = "Qualifiche",
            ["ar"] = "المؤهلات",
            ["cs"] = "Kvalifikace",
            ["da"] = "Kvalifikationer",
            ["el"] = "Προσόντα",
            ["es"] = "Cualificaciones",
            ["fi"] = "Pätevyydet",
            ["he"] = "הסמכות",
            ["id"] = "Kualifikasi",
            ["ja"] = "資格",
            ["ko"] = "자격",
            ["ms"] = "Kelayakan",
            ["nb"] = "Kvalifikasjoner",
            ["nl"] = "Kwalificaties",
            ["pl"] = "Kwalifikacje",
            ["pt"] = "Qualificações",
            ["ro"] = "Calificări",
            ["sv"] = "Kvalifikationer",
            ["th"] = "คุณสมบัติ",
            ["vi"] = "Trình độ chuyên môn",
            ["zh-CN"] = "资质",
            ["zh-TW"] = "資格",
        };

    public static string Resolve(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return ByLanguage[FallbackLanguage];
        }

        var tag = language.Trim();
        if (ByLanguage.TryGetValue(tag, out var exact))
        {
            return exact;
        }

        var separatorIndex = tag.IndexOf(RegionSeparator);
        if (separatorIndex > 0 && ByLanguage.TryGetValue(tag[..separatorIndex], out var baseLanguageName))
        {
            return baseLanguageName;
        }

        return ByLanguage[FallbackLanguage];
    }
}
