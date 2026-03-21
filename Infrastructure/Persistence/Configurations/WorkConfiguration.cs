// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die Work-Entity mit QueryFilter, Indizes und Beziehungen zu Client/Shift.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.ShiftId });
        builder.HasIndex(p => new { p.CurrentDate, p.ClientId, p.IsDeleted });
        builder.HasIndex(p => p.AnalyseToken).HasFilter("analyse_token IS NOT NULL");

        builder.HasOne(p => p.Client)
            .WithMany(b => b.Works)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Shift);
    }
}
