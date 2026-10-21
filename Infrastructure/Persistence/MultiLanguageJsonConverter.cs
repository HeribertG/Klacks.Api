// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core ValueConverter for MultiLanguage JSONB columns (the matching comparer is MultiLanguageValueComparer).
/// Serializes all languages (core + plugin) dynamically via MultiLanguageSystemTextJsonConverter.
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Klacks.Api.Infrastructure.Persistence;

public class MultiLanguageJsonConverter : ValueConverter<MultiLanguage, string>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MultiLanguageJsonConverter() : base(
        v => Serialize(v),
        v => Deserialize(v),
        convertsNulls: true)
    { }

    private static string Serialize(MultiLanguage? multiLanguage)
    {
        if (multiLanguage == null || multiLanguage.IsEmpty)
            return "{}";

        return JsonSerializer.Serialize(multiLanguage, SerializerOptions);
    }

    private static MultiLanguage Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json) || json == "{}")
            return new MultiLanguage();

        return JsonSerializer.Deserialize<MultiLanguage>(json, SerializerOptions)
            ?? new MultiLanguage();
    }
}
