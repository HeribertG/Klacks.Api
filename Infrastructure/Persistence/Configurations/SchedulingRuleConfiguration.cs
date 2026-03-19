// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die SchedulingRule-Entity mit QueryFilter und Index.
/// </summary>
using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SchedulingRuleConfiguration : IEntityTypeConfiguration<SchedulingRule>
{
    public void Configure(EntityTypeBuilder<SchedulingRule> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.Name });
    }
}
