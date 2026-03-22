// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the LLMProvider-Entity with query filter and JSONB settings.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMProviderConfiguration : IEntityTypeConfiguration<LLMProvider>
{
    public void Configure(EntityTypeBuilder<LLMProvider> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.Settings).HasJsonbConversionWithComparer<Dictionary<string, object>>();
    }
}
