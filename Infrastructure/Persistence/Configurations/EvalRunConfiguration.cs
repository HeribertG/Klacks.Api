// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for EvalRun with table name, query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EvalRunConfiguration : IEntityTypeConfiguration<EvalRun>
{
    public void Configure(EntityTypeBuilder<EvalRun> builder)
    {
        builder.ToTable("eval_runs");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.Goldset, p.CreateTime });
        builder.Property(p => p.Goldset).HasMaxLength(64);
        builder.Property(p => p.Provider).HasMaxLength(32);
        builder.Property(p => p.Model).HasMaxLength(64);
        builder.Property(p => p.CompositeScore).HasPrecision(6, 4);
        builder.Property(p => p.RegressionVsBaseline).HasPrecision(6, 4);
        builder.Property(p => p.DimensionsJson).HasColumnType("jsonb");
    }
}
