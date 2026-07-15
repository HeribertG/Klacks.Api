// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for CounterRule: ImportSourceKey is unique among non-deleted rows (partial
/// index, matching the project's soft-delete unique-constraint convention). Rows are exclusively
/// import-created, so no empty-key exclusion is needed.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CounterRuleConfiguration : IEntityTypeConfiguration<CounterRule>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<CounterRule> builder)
    {
        builder.Property(r => r.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength);
        builder.Property(r => r.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
