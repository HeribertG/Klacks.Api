// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for RestDayRotationRule: ImportSourceKey is unique among non-deleted rows
/// (partial index, matching the project's soft-delete unique-constraint convention) so the K20
/// entity-import reconciliation can look a row up by its natural key without colliding with a
/// soft-deleted predecessor. Rows are exclusively import-created, so no empty-key exclusion is needed.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class RestDayRotationRuleConfiguration : IEntityTypeConfiguration<RestDayRotationRule>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<RestDayRotationRule> builder)
    {
        builder.Property(r => r.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength);
        builder.Property(r => r.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
