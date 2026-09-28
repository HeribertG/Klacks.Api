// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a goldset item id to the default-goldset item it was derived from: a translation
/// (i18n-&lt;locale&gt;--&lt;sourceId&gt;) and a paraphrase (para-&lt;sourceId&gt;-&lt;n&gt;) both stand for their source, any
/// other id for itself. Evidence counts use it so that one German miss translated into many languages, or
/// paraphrased several times, weighs as one observed miss and not as many.
/// </summary>
/// <param name="itemId">A goldset item id of any learning goldset</param>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetItemSource
{
    private const char ParaphraseSuffixSeparator = '-';

    public static string Resolve(string itemId)
    {
        if (GoldsetTranslationId.TryParse(itemId, out _, out var sourceId))
        {
            return sourceId;
        }

        if (itemId.StartsWith(TurnEvalDefaults.ParaphraseItemIdPrefix, StringComparison.Ordinal))
        {
            var rest = itemId.Substring(TurnEvalDefaults.ParaphraseItemIdPrefix.Length);
            var suffixIndex = rest.LastIndexOf(ParaphraseSuffixSeparator);
            return suffixIndex > 0 ? rest.Substring(0, suffixIndex) : rest;
        }

        return itemId;
    }
}
