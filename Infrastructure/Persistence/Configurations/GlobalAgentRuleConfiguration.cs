// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the GlobalAgentRule-Entity with query filter and gefilterten unique indexes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GlobalAgentRuleConfiguration : IEntityTypeConfiguration<GlobalAgentRule>
{
    public void Configure(EntityTypeBuilder<GlobalAgentRule> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Name)
            .HasFilter("is_active = true AND is_deleted = false")
            .IsUnique();
        builder.HasIndex(p => p.SortOrder)
            .HasFilter("is_active = true AND is_deleted = false");
    }
}
