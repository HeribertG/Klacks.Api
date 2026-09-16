// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for AssistantLastActionRow: table name, explicit primary key (the row is not a
/// BaseEntity, so no key is inferred and no soft-delete query filter applies), bounded text columns and
/// a unique index on (UserId, ConversationId) so at most one previous action exists per conversation.
/// The length limits are mirrored in the store's own capping - EF InMemory ignores HasMaxLength.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AssistantLastActionRowConfiguration : IEntityTypeConfiguration<AssistantLastActionRow>
{
    public void Configure(EntityTypeBuilder<AssistantLastActionRow> builder)
    {
        builder.ToTable("assistant_last_actions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ConversationId).HasMaxLength(GracefulCorrectionDefaults.ConversationIdMaxLength);
        builder.Property(p => p.UserMessage).HasMaxLength(GracefulCorrectionDefaults.UserMessageMaxLength);
        builder.Property(p => p.AssistantAnswerExcerpt).HasMaxLength(GracefulCorrectionDefaults.AnswerExcerptMaxLength);
        builder.HasIndex(p => new { p.UserId, p.ConversationId }).IsUnique();
        builder.HasIndex(p => p.ExpiresAtUtc);
    }
}
