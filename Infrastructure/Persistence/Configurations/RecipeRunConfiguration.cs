// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for RecipeRun: soft-delete query filter and the indexes the three read
/// paths need — resuming a run of one conversation, the per-user funnel of the effectiveness
/// endpoint, and the background sweep that scans Running rows by age.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class RecipeRunConfiguration : IEntityTypeConfiguration<RecipeRun>
{
    public void Configure(EntityTypeBuilder<RecipeRun> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.ConversationId, p.Status });
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.UpdateTime);

        builder.Property(p => p.AbortReason).HasMaxLength(RecipeRunDefaults.AbortReasonMaxLength);
    }
}
