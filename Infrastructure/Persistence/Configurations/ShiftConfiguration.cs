// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the Shift-Entity with query filter, indexes and Client-relationship.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.MacroId, p.ClientId, p.Status, p.FromDate, p.UntilDate });
        builder.HasIndex(p => p.AnalyseToken).HasFilter("analyse_token IS NOT NULL");

        builder.HasIndex(p => p.ScenarioSourceShiftId)
            .HasFilter("scenario_source_shift_id IS NOT NULL AND is_deleted = false");

        // Not unique: an ExternalOrderReference forms a version chain over time (superseded orders
        // stay non-deleted with an UntilDate), so more than one Shift row can legitimately share it.
        builder.HasIndex(p => new { p.SourceSystemId, p.ExternalOrderReference }, ShiftIndexNames.ExternalOrderReferenceLookup)
            .HasDatabaseName(ShiftIndexNames.ExternalOrderReferenceLookup)
            .HasFilter("external_order_reference IS NOT NULL");

        // One open draft per (source system, reference). Deliberately narrow: supersession keeps the closed
        // SealedOrder row with the same reference, so only OriginalOrder rows may not repeat.
        builder.HasIndex(p => new { p.SourceSystemId, p.ExternalOrderReference }, ShiftIndexNames.OpenDraftReferenceUnique)
            .IsUnique()
            .HasDatabaseName(ShiftIndexNames.OpenDraftReferenceUnique)
            .HasFilter($"external_order_reference IS NOT NULL AND is_deleted = false AND status = {(int)ShiftStatus.OriginalOrder}");

        builder.HasOne(s => s.Client)
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Shift>()
            .WithMany()
            .HasForeignKey(s => s.ScenarioSourceShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
