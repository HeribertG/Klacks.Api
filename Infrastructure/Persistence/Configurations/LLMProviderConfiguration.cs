// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die LLMProvider-Entity mit QueryFilter und JSONB-Settings.
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
