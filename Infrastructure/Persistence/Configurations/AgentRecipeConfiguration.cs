// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the AgentRecipe entity with soft-delete query filter, a unique
/// name index over non-deleted rows and JSONB per-language trigger synonyms, pack veto vocabulary,
/// pack anchor vocabulary and goal translations.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentRecipeConfiguration : IEntityTypeConfiguration<AgentRecipe>
{
    private const int OriginMaxLength = 16;

    public void Configure(EntityTypeBuilder<AgentRecipe> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Name)
            .HasFilter("is_deleted = false")
            .IsUnique();
        builder.HasIndex(p => new { p.IsEnabled, p.SortOrder });
        builder.Property(p => p.Origin).HasMaxLength(OriginMaxLength).IsRequired();
        builder.Property(e => e.Synonyms)
            .HasJsonbConversionWithComparer<Dictionary<string, List<string>>>();
        builder.Property(e => e.Vetoes)
            .HasJsonbConversionWithComparer<Dictionary<string, List<string>>>();
        builder.Property(e => e.Anchors)
            .HasJsonbConversionWithComparer<Dictionary<string, List<string>>>();
        builder.Property(e => e.GoalTranslations)
            .HasJsonbConversionWithComparer<Dictionary<string, string>>();
    }
}
