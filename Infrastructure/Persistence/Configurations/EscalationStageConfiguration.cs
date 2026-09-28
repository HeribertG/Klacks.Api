// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the EscalationStage entity. The (ChainId, Rank) index backs the sweep's
/// per-chain ordering; the (UserId, Status) index backs the reply path, which looks up "the stage
/// this replying user is currently Notified on" without a chain id in hand.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Escalation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EscalationStageConfiguration : IEntityTypeConfiguration<EscalationStage>
{
    public void Configure(EntityTypeBuilder<EscalationStage> builder)
    {
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => new { s.EscalationChainId, s.Rank }).IsUnique();
        builder.HasIndex(s => new { s.UserId, s.Status });
        builder.HasIndex(s => s.DueAtUtc);
    }
}
