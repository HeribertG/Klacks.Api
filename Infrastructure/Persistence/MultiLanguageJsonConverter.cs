// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core ValueConverter und ValueComparer für MultiLanguage JSONB-Spalten.
/// Serialisiert alle Sprachen (Core + Plugin) dynamisch via MultiLanguageSystemTextJsonConverter.
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Klacks.Api.Infrastructure.Persistence;

public class MultiLanguageJsonConverter : ValueConverter<MultiLanguage, string>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MultiLanguageJsonConverter() : base(
        v => JsonSerializer.Serialize(v, SerializerOptions),
        v => Deserialize(v))
    { }

    private static MultiLanguage Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new MultiLanguage();

        return JsonSerializer.Deserialize<MultiLanguage>(json, SerializerOptions)
            ?? new MultiLanguage();
    }
}

public class MultiLanguageValueComparer : ValueComparer<MultiLanguage>
{
    public MultiLanguageValueComparer() : base(
        (a, b) => AreEqual(a, b),
        v => ComputeHash(v),
        v => Clone(v))
    { }

    private static bool AreEqual(MultiLanguage? a, MultiLanguage? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        var aValues = a.GetAllValues();
        var bValues = b.GetAllValues();
        if (aValues.Count != bValues.Count) return false;
        return aValues.All(kvp => bValues.TryGetValue(kvp.Key, out var val) && val == kvp.Value);
    }

    private static int ComputeHash(MultiLanguage v)
    {
        var hash = new HashCode();
        foreach (var kvp in v.GetAllValues().OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    private static MultiLanguage Clone(MultiLanguage v)
    {
        var clone = new MultiLanguage();
        foreach (var kvp in v.GetAllValues())
        {
            clone.SetValue(kvp.Key, kvp.Value);
        }
        return clone;
    }
}
