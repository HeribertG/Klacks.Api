// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die LLMUsage-Entity mit QueryFilter.
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
    }
}
