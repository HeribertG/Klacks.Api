// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for PendingConfirmationRow: table name, explicit primary key (the row is not a
/// BaseEntity, so no key is inferred and no soft-delete query filter applies), bounded key columns,
/// a unique index on Token (the natural lookup/consume key) and supporting indexes on UserId and
/// ExpiresAtUtc for PeekLatestForUser and TTL pruning.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PendingConfirmationRowConfiguration : IEntityTypeConfiguration<PendingConfirmationRow>
{
    public void Configure(EntityTypeBuilder<PendingConfirmationRow> builder)
    {
        builder.ToTable("pending_confirmations");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Token).HasMaxLength(64);
        builder.Property(p => p.SkillName).HasMaxLength(256);
        builder.Property(p => p.Purpose)
            .HasMaxLength(PendingConfirmationPurposes.MaxLength)
            .HasDefaultValue(PendingConfirmationPurposes.GateReplay);
        builder.HasIndex(p => p.Token).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.ExpiresAtUtc);
    }
}
