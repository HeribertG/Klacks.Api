// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the WorkSoftening entity. Soft-delete query filter, scenario-
/// scoped composite index for token-based cleanup and harmonizer lookup.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class WorkSofteningConfiguration : IEntityTypeConfiguration<WorkSoftening>
{
    public void Configure(EntityTypeBuilder<WorkSoftening> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.RuleName).HasMaxLength(64);
        builder.Property(p => p.Hint).HasMaxLength(512);
        builder.Property(p => p.Kind).HasConversion<byte>();
        builder.HasIndex(p => new { p.IsDeleted, p.ClientId, p.CurrentDate, p.AnalyseToken });
        builder.HasIndex(p => new { p.IsDeleted, p.AnalyseToken });
    }
}
