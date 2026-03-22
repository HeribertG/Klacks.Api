// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the AnalyseScenario entity with query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AnalyseScenarioConfiguration : IEntityTypeConfiguration<AnalyseScenario>
{
    public void Configure(EntityTypeBuilder<AnalyseScenario> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Token).IsUnique();
        builder.HasIndex(p => new { p.GroupId, p.Status });
    }
}
