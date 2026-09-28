// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Static DB functions for MultiLanguage JSONB columns.
/// Used in LINQ queries and translated by EF Core to PostgreSQL jsonb_extract_path_text.
/// @param column - The MultiLanguage JSONB column
/// @param key - The language key (e.g. "de", "en", "ja")
/// </summary>
namespace Klacks.Api.Domain.Common;

public static class MultiLanguageDbFunctions
{
    public static string? ExtractText(MultiLanguage? column, string key)
        => column?.GetValue(key);
}
