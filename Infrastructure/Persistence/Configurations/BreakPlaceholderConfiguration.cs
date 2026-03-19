// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die BreakPlaceholder-Entity mit QueryFilter, Indizes und Absence-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class BreakPlaceholderConfiguration : IEntityTypeConfiguration<BreakPlaceholder>
{
    public void Configure(EntityTypeBuilder<BreakPlaceholder> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.AbsenceId, p.ClientId });
        builder.HasIndex(p => new { p.IsDeleted, p.ClientId, p.From, p.Until });

        builder.HasOne(p => p.Absence)
            .WithMany()
            .HasForeignKey(p => p.AbsenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
