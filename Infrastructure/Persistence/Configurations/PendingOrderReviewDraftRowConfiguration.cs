// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF configuration for PendingOrderReviewDraftRow: table name, explicit primary key (the row is not
/// a BaseEntity, so no key is inferred and no soft-delete query filter applies), bounded key columns and
/// a unique index on (UserId, ConversationId) so at most one draft exists per conversation.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PendingOrderReviewDraftRowConfiguration : IEntityTypeConfiguration<PendingOrderReviewDraftRow>
{
    public void Configure(EntityTypeBuilder<PendingOrderReviewDraftRow> builder)
    {
        builder.ToTable("pending_order_review_drafts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ConversationId).HasMaxLength(128);
        builder.HasIndex(p => new { p.UserId, p.ConversationId }).IsUnique();
        builder.HasIndex(p => p.ExpiresAtUtc);
    }
}
