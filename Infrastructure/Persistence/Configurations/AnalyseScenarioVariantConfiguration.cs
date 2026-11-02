// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the AnalyseScenarioVariant link with soft-delete query filter, a unique scenario/variant pair and the variant foreign key.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AnalyseScenarioVariantConfiguration : IEntityTypeConfiguration<AnalyseScenarioVariant>
{
    public void Configure(EntityTypeBuilder<AnalyseScenarioVariant> builder)
    {
        builder.ToTable("analyse_scenario_variants");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ScenarioToken, p.VariantId })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
        builder.HasIndex(p => p.VariantId);
        builder.HasOne(p => p.Variant)
            .WithMany()
            .HasForeignKey(p => p.VariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
