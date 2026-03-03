// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Klacks.Api.Infrastructure.Persistence.Converters;

public class MultiLanguageJsonConverter : ValueConverter<MultiLanguage?, string?>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public MultiLanguageJsonConverter() : base(
        v => Serialize(v),
        v => Deserialize(v))
    {
    }

    private static string? Serialize(MultiLanguage? multiLanguage)
    {
        if (multiLanguage == null || multiLanguage.IsEmpty)
        {
            return null;
        }

        return JsonSerializer.Serialize(multiLanguage.ToDictionary(), SerializerOptions);
    }

    private static MultiLanguage? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions);
            if (dict == null || dict.Count == 0)
            {
                return null;
            }

            var ml = new MultiLanguage();
            foreach (var (key, value) in dict)
            {
                ml.SetValue(key, value);
            }
            return ml;
        }
        catch
        {
            return null;
        }
    }
}
