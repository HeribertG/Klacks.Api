// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Extensions für MultiLanguage JSONB-Property-Konfiguration.
/// Ersetzt OwnsOne().ToJson() durch ValueConverter-basierte Konfiguration,
/// die alle Sprachen (Core + Plugin) dynamisch unterstützt.
/// </summary>
using System.Linq.Expressions;
using Klacks.Api.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence;

public static class MultiLanguageEntityTypeBuilderExtensions
{
    public static void ConfigureMultiLanguage<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, MultiLanguage?>> property,
        string columnName) where TEntity : class
    {
        var propertyBuilder = builder.Property(property);
        ApplyMultiLanguageConfig(propertyBuilder, columnName);
    }

    public static void RegisterMultiLanguageDbFunctions(this ModelBuilder modelBuilder)
    {
        var extractMethod = typeof(MultiLanguageDbFunctions)
            .GetMethod(nameof(MultiLanguageDbFunctions.ExtractText),
                [typeof(MultiLanguage), typeof(string)])!;

        var dbFunction = modelBuilder.HasDbFunction(extractMethod)
            .HasName("jsonb_extract_path_text");

        dbFunction.HasParameter("column").HasStoreType("jsonb");
    }

    private static void ApplyMultiLanguageConfig<T>(PropertyBuilder<T> propertyBuilder, string columnName)
    {
        propertyBuilder
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(new MultiLanguageJsonConverter());
        propertyBuilder.Metadata.SetValueComparer(new MultiLanguageValueComparer());
    }
}
