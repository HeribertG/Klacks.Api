// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds and parses the id of a translated goldset item: i18n-&lt;locale&gt;--&lt;sourceId&gt;. The double hyphen is
/// the only separator, because both halves may contain single hyphens (locale zh-CN, source w05-recipe-...);
/// parsing therefore needs no list of known locales. Matching is ordinal and case-sensitive, like the
/// paraphrase prefix: an id that merely resembles the format is not a translation.
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetTranslationId
{
    public const string SourceSeparator = "--";

    public static string Compose(string locale, string sourceId) =>
        TurnEvalDefaults.I18nItemIdPrefix + locale + SourceSeparator + sourceId;

    public static bool IsTranslationId(string? itemId) =>
        itemId != null && itemId.StartsWith(TurnEvalDefaults.I18nItemIdPrefix, StringComparison.Ordinal);

    public static bool TryParse(string? itemId, out string locale, out string sourceId)
    {
        locale = string.Empty;
        sourceId = string.Empty;

        if (!IsTranslationId(itemId))
        {
            return false;
        }

        var rest = itemId!.Substring(TurnEvalDefaults.I18nItemIdPrefix.Length);
        var separatorIndex = rest.IndexOf(SourceSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var parsedSource = rest.Substring(separatorIndex + SourceSeparator.Length);
        if (string.IsNullOrWhiteSpace(parsedSource))
        {
            return false;
        }

        locale = rest.Substring(0, separatorIndex);
        sourceId = parsedSource;
        return true;
    }
}
