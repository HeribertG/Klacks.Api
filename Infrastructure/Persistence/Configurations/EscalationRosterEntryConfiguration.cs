// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the EscalationRosterEntry entity. One row per (group root, user); the
/// unique index is what makes the re-derivation an upsert instead of an accumulate.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Escalation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EscalationRosterEntryConfiguration : IEntityTypeConfiguration<EscalationRosterEntry>
{
    public void Configure(EntityTypeBuilder<EscalationRosterEntry> builder)
    {
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.GroupRootId, r.UserId }).IsUnique();
    }
}
