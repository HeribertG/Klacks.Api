// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die ScheduleNote-Entity mit QueryFilter und AnalyseToken-Index.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ScheduleNoteConfiguration : IEntityTypeConfiguration<ScheduleNote>
{
    public void Configure(EntityTypeBuilder<ScheduleNote> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.AnalyseToken).HasFilter("analyse_token IS NOT NULL");
    }
}
