// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for RestrictedTimeWindowRule (K16). ImportSourceKey is the natural key the
/// region-setup re-apply reconciles by; it is enforced as a partial unique index over non-deleted rows
/// with a non-empty key, so a future customer-CRUD row (empty ImportSourceKey) never collides and a
/// soft-deleted predecessor never blocks a re-created row for the same key.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class RestrictedTimeWindowRuleConfiguration : IEntityTypeConfiguration<RestrictedTimeWindowRule>
{
    public void Configure(EntityTypeBuilder<RestrictedTimeWindowRule> builder)
    {
        builder.Property(r => r.AppliesToGroupTag).HasMaxLength(200);
        builder.Property(r => r.ImportSourceKey).HasMaxLength(200);
        builder.Property(r => r.ImportContentHash).HasMaxLength(64);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
