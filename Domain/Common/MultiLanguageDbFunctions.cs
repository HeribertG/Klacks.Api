// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Statische DB-Funktionen für MultiLanguage JSONB-Spalten.
/// Werden in LINQ-Queries verwendet und von EF Core zu PostgreSQL jsonb_extract_path_text übersetzt.
/// @param column - Die MultiLanguage JSONB-Spalte
/// @param key - Der Sprachschlüssel (z.B. "de", "en", "ja")
/// </summary>
namespace Klacks.Api.Domain.Common;

public static class MultiLanguageDbFunctions
{
    public static string? ExtractText(MultiLanguage? column, string key)
        => column?.GetValue(key);
}
