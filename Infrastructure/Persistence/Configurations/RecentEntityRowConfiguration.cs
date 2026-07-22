// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for RecentEntityRow: table name, explicit primary key (the row is not a BaseEntity,
/// so no key is inferred and no soft-delete query filter applies), bounded string columns, and an index
/// on (UserId, ConversationId, CreatedAtUtc) supporting the newest-first ring read and trim.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class RecentEntityRowConfiguration : IEntityTypeConfiguration<RecentEntityRow>
{
    public void Configure(EntityTypeBuilder<RecentEntityRow> builder)
    {
        builder.ToTable("conversation_recent_entities");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ConversationId).HasMaxLength(128);
        builder.Property(r => r.EntityType).HasMaxLength(64);
        builder.Property(r => r.DisplayName).HasMaxLength(256);
        builder.Property(r => r.Action).HasMaxLength(16);
        builder.HasIndex(r => new { r.UserId, r.ConversationId, r.CreatedAtUtc });
    }
}
