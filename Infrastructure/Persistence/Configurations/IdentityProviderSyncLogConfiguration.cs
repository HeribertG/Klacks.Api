// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the IdentityProviderSyncLog-Entity with query filter, indexes and relationshipen.
/// </summary>
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class IdentityProviderSyncLogConfiguration : IEntityTypeConfiguration<IdentityProviderSyncLog>
{
    public void Configure(EntityTypeBuilder<IdentityProviderSyncLog> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IdentityProviderId, p.ExternalId });
        builder.HasIndex(p => new { p.ClientId, p.IdentityProviderId });

        builder.HasOne(s => s.IdentityProvider)
            .WithMany()
            .HasForeignKey(s => s.IdentityProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Client)
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
