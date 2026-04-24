// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the WizardTrainingRun entity: indexes for typical training queries.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class WizardTrainingRunConfiguration : IEntityTypeConfiguration<WizardTrainingRun>
{
    public void Configure(EntityTypeBuilder<WizardTrainingRun> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.CreateTime);
        builder.HasIndex(p => new { p.Source, p.CreateTime });
        builder.HasIndex(p => p.Stage2Score);
    }
}
