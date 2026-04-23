// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Work-Entity with query filter, indexes and relationships to Client/Shift.
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

        // Remap CurrentDate to a non-reserved column name. "current_date" is a SQL reserved
        // identifier in PostgreSQL; Npgsql 10.x silently replaces parameter values bound to
        // such a column with CURRENT_DATE on INSERT/UPDATE. See RenameCurrentDateToWorkday migration.
        builder.Property(p => p.CurrentDate).HasColumnName("workday");

        builder.HasOne(p => p.Client)
            .WithMany(b => b.Works)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Shift);
    }
}
