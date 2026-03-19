// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AbsenceDetail-Entity mit QueryFilter, MultiLanguage, Index und Absence-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AbsenceDetailConfiguration : IEntityTypeConfiguration<AbsenceDetail>
{
    public void Configure(EntityTypeBuilder<AbsenceDetail> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.ConfigureMultiLanguage(a => a.DetailName, "detail_name");
        builder.ConfigureMultiLanguage(a => a.Description, "description");
        builder.HasIndex(p => new { p.IsDeleted, p.AbsenceId });

        builder.HasOne(p => p.Absence)
            .WithMany()
            .HasForeignKey(p => p.AbsenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
