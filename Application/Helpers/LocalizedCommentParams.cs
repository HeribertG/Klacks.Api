// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes and resolves translation parameters that carry a multilingual name (see LocalizedCommentParamKeys).
/// Add stores the name under its key as readable text and, under the key plus the MultiLanguage suffix, as the
/// serialized MultiLanguage; ForLanguage turns such a parameter set into plain parameters for one known reader
/// (an assistant answer), so the model sees one name in the user's language instead of every translation.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Helpers;

public static class LocalizedCommentParams
{
    /// <summary>
    /// Stores a multilingual name as a translation parameter.
    /// </summary>
    /// <param name="parameters">Parameter set of the finding</param>
    /// <param name="key">Plain parameter key the translation text interpolates (e.g. "holiday")</param>
    /// <param name="name">The name in every language it is known in</param>
    public static void Add(IDictionary<string, string> parameters, string key, MultiLanguage name)
    {
        parameters[key] = name.GetValueOrFirstAvailable(null);
        parameters[key + LocalizedCommentParamKeys.MultiLanguageSuffix] = JsonSerializer.Serialize(name.ToDictionary());
    }

    /// <summary>
    /// Returns a copy of the parameters in which every multilingual parameter is resolved to the given language
    /// and its serialized companion is dropped. Parameters without a companion are copied unchanged.
    /// </summary>
    /// <param name="parameters">Parameter set of the finding</param>
    /// <param name="language">The reader's language (e.g. "ja", "zh-CN")</param>
    public static Dictionary<string, string> ForLanguage(IReadOnlyDictionary<string, string> parameters, string? language)
    {
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in parameters)
        {
            if (!key.EndsWith(LocalizedCommentParamKeys.MultiLanguageSuffix, StringComparison.Ordinal))
            {
                result.TryAdd(key, value);
                continue;
            }

            var resolved = Parse(value)?.GetValueOrFirstAvailable(language);
            if (!string.IsNullOrEmpty(resolved))
            {
                result[key[..^LocalizedCommentParamKeys.MultiLanguageSuffix.Length]] = resolved;
            }
        }

        return result;
    }

    private static MultiLanguage? Parse(string json)
    {
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (values == null)
            {
                return null;
            }

            var name = new MultiLanguage();
            foreach (var (language, text) in values)
            {
                name.SetValue(language, text);
            }

            return name;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
