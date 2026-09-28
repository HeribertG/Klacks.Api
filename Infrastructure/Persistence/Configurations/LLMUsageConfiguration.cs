// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the LLMUsage-Entity with query filter.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMUsageConfiguration : IEntityTypeConfiguration<LLMUsage>
{
    public void Configure(EntityTypeBuilder<LLMUsage> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.ToolChoiceRequested).HasDefaultValue(false);
        builder.Property(p => p.ToolChoiceSupported).HasDefaultValue(false);
        builder.Property(p => p.ToolCallReturned).HasDefaultValue(false);
    }
}
