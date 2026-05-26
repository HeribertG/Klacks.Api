// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the NavigationTargetSynonym entity with table name, query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class NavigationTargetSynonymConfiguration : IEntityTypeConfiguration<NavigationTargetSynonym>
{
    public void Configure(EntityTypeBuilder<NavigationTargetSynonym> builder)
    {
        builder.ToTable("navigation_target_synonyms");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.TargetId, p.Language });
        builder.HasIndex(p => new { p.TargetId, p.Language, p.Keyword })
            .HasFilter("is_deleted = false")
            .IsUnique();
    }
}
