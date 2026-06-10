// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for AgentAutonomyPreferenceRow: table name, soft-delete query filter,
/// unique index on UserId so each user has at most one autonomy level row.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentAutonomyPreferenceRowConfiguration : IEntityTypeConfiguration<AgentAutonomyPreferenceRow>
{
    public void Configure(EntityTypeBuilder<AgentAutonomyPreferenceRow> builder)
    {
        builder.ToTable("agent_autonomy_preferences");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.UserId).HasMaxLength(64);
        builder.HasIndex(p => p.UserId)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
