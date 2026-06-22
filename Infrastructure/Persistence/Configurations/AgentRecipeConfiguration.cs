// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the AgentRecipe entity with soft-delete query filter and a unique
/// name index over non-deleted rows.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentRecipeConfiguration : IEntityTypeConfiguration<AgentRecipe>
{
    public void Configure(EntityTypeBuilder<AgentRecipe> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Name)
            .HasFilter("is_deleted = false")
            .IsUnique();
        builder.HasIndex(p => new { p.IsEnabled, p.SortOrder });
    }
}
