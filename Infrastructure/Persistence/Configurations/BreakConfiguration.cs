// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Break entity with query filter, MultiLanguage, indexes and relationships.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class BreakConfiguration : IEntityTypeConfiguration<Break>
{
    public void Configure(EntityTypeBuilder<Break> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.ConfigureMultiLanguage(b => b.Description, "description");
        builder.HasIndex(p => new { p.ClientId });
        builder.HasIndex(p => new { p.CurrentDate, p.ClientId });
        builder.HasIndex(p => p.AnalyseToken).HasFilter("analyse_token IS NOT NULL");

        builder.HasOne(p => p.Client)
            .WithMany(b => b.Breaks)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Absence)
            .WithMany()
            .HasForeignKey(p => p.AbsenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
