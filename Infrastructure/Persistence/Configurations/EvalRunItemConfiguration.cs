// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for EvalRunItem. The second index serves the only query the learning loop runs
/// against this table: the unconsumed pure selection misses of one run.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EvalRunItemConfiguration : IEntityTypeConfiguration<EvalRunItem>
{
    public void Configure(EntityTypeBuilder<EvalRunItem> builder)
    {
        builder.ToTable("eval_run_items");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.ItemId).HasMaxLength(TurnEvalDefaults.ItemIdMaxLength).IsRequired();
        builder.Property(p => p.Locale).HasMaxLength(TurnEvalDefaults.LocaleMaxLength);
        builder.Property(p => p.ExpectedTool).HasMaxLength(TurnEvalDefaults.ToolNameMaxLength);
        builder.Property(p => p.ChosenTool).HasMaxLength(TurnEvalDefaults.ToolNameMaxLength);
        builder.Property(p => p.ToolsetNamesJson).HasColumnType("jsonb");

        builder.HasIndex(p => p.EvalRunId);
        builder.HasIndex(p => new { p.EvalRunId, p.RetrievalHit, p.SelectionHit, p.LearningConsumedAtUtc });

        builder.HasOne<EvalRun>()
            .WithMany()
            .HasForeignKey(p => p.EvalRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
