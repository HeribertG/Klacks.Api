// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for InboundClarification: one row per question Klacksy sent (or only
/// suggested) to an employee about an unclear inbound message. The partial unique index enforces at
/// most one Open clarification per client at the database level, so two concurrent inbound messages of
/// the same employee can never both raise a question. Resolved and soft-deleted rows are excluded from
/// the index (stored-procedures.md: unique indexes on soft-delete tables must be partial). No foreign
/// keys: ClientId, OriginalAnalysisId, OriginalSourceId, AnswerSourceId and ResultAnalysisId are plain
/// values like on inbound_analyses, because a messenger source row lives in a plugin-owned table.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Inbound;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class InboundClarificationConfiguration : IEntityTypeConfiguration<InboundClarification>
{
    internal const string OpenPerClientIndexName = "ix_inbound_clarifications_client_id_open";
    internal const string OpenPerClientIndexFilter = "status = 0 AND is_deleted = false";

    private const int ChannelMaxLength = 64;
    private const int RecipientMaxLength = InboundClarificationConstants.MaxRecipientLength;
    private const int SenderDisplayMaxLength = 300;
    private const int QuestionMaxLength = 1000;
    private const int ShiftContextMaxLength = InboundClarificationConstants.MaxShiftContextLength;
    private const int EmailMessageIdMaxLength = InboundClarificationConstants.MaxStoredEmailMessageIdLength;

    public void Configure(EntityTypeBuilder<InboundClarification> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.Channel).HasMaxLength(ChannelMaxLength);
        builder.Property(p => p.Recipient).HasMaxLength(RecipientMaxLength);
        builder.Property(p => p.SenderDisplay).HasMaxLength(SenderDisplayMaxLength);
        builder.Property(p => p.Question).HasMaxLength(QuestionMaxLength);
        builder.Property(p => p.ShiftContext).HasMaxLength(ShiftContextMaxLength);
        builder.Property(p => p.EmailMessageId).HasMaxLength(EmailMessageIdMaxLength);

        builder.HasIndex(p => p.ClientId)
            .IsUnique()
            .HasFilter(OpenPerClientIndexFilter)
            .HasDatabaseName(OpenPerClientIndexName);
        builder.HasIndex(p => new { p.Status, p.DeadlineAt });
        builder.HasIndex(p => new { p.ClientId, p.AskedAt });
        builder.HasIndex(p => p.OriginalAnalysisId);
        builder.HasIndex(p => p.ResultAnalysisId);
    }
}
