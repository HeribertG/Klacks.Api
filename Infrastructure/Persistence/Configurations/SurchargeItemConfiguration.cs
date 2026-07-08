// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SurchargeItem entity: three optional parent relationships
/// (Work, Break, WorkChange), an exactly-one-parent check constraint and foreign key indexes.
/// SurchargeItem is recomputable derived data and intentionally not soft-deleted (no query filter).
/// </summary>
/// <param name="builder">Configures the Work/Break/WorkChange relationships and the single-parent constraint</param>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SurchargeItemConfiguration : IEntityTypeConfiguration<SurchargeItem>
{
    public void Configure(EntityTypeBuilder<SurchargeItem> builder)
    {
        builder.HasKey(si => si.Id);

        builder.Property(si => si.Type).IsRequired();
        builder.Property(si => si.Amount).IsRequired();

        builder.HasOne(si => si.Work)
            .WithMany(w => w.SurchargeItems)
            .HasForeignKey(si => si.WorkId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(si => si.Break)
            .WithMany(b => b.SurchargeItems)
            .HasForeignKey(si => si.BreakId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(si => si.WorkChange)
            .WithMany(wc => wc.SurchargeItems)
            .HasForeignKey(si => si.WorkChangeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(si => si.WorkId);
        builder.HasIndex(si => si.BreakId);
        builder.HasIndex(si => si.WorkChangeId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SurchargeItem_ExactlyOneParent",
            "((CASE WHEN work_id IS NOT NULL THEN 1 ELSE 0 END) + " +
            "(CASE WHEN break_id IS NOT NULL THEN 1 ELSE 0 END) + " +
            "(CASE WHEN work_change_id IS NOT NULL THEN 1 ELSE 0 END)) = 1"));
    }
}
