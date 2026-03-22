// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the IdentityProvider-Entity with query filter, JSONB attribute mapping and Index.
/// </summary>
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class IdentityProviderConfiguration : IEntityTypeConfiguration<IdentityProvider>
{
    public void Configure(EntityTypeBuilder<IdentityProvider> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.AttributeMapping).HasJsonbConversionWithComparer<Dictionary<string, string>>();
        builder.HasIndex(p => new { p.IsDeleted, p.IsEnabled, p.SortOrder });
    }
}
