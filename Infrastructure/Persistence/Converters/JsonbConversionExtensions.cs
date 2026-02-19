using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Converters;

public static class JsonbConversionExtensions
{
    public static PropertyBuilder<T?> HasJsonbConversion<T>(this PropertyBuilder<T?> builder) where T : class
    {
        return builder
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? default : JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
    }

    public static PropertyBuilder<T?> HasJsonbConversionWithComparer<T>(this PropertyBuilder<T?> builder) where T : class
    {
        builder.HasJsonbConversion<T>();

        builder.Metadata.SetValueComparer(new ValueComparer<T?>(
            (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
            c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => c == null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)));

        return builder;
    }

    public static PropertyBuilder<List<T>?> HasJsonbListConversion<T>(this PropertyBuilder<List<T>?> builder)
    {
        return builder
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new() : JsonSerializer.Deserialize<List<T>>(v, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("jsonb");
    }
}
