// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Shift-Entity with query filter, indexes and Client-relationship.
/// </summary>
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
        builder.HasIndex(p => new { p.SourceSystemId, p.ExternalOrderReference })
            .HasFilter("external_order_reference IS NOT NULL");

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
