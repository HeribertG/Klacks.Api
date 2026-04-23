// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ScheduleCommand entity with query filter and AnalyseToken index.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ScheduleCommandConfiguration : IEntityTypeConfiguration<ScheduleCommand>
{
    public void Configure(EntityTypeBuilder<ScheduleCommand> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.AnalyseToken).HasFilter("analyse_token IS NOT NULL");

        // See WorkConfiguration: "current_date" is a reserved PostgreSQL identifier.
        builder.Property(p => p.CurrentDate).HasColumnName("workday");
    }
}
