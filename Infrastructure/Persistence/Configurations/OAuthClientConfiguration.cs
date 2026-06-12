// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the OAuthClient entity with soft-delete query filter and a
/// partial unique index on the public client id.
/// </summary>
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class OAuthClientConfiguration : IEntityTypeConfiguration<OAuthClient>
{
    public void Configure(EntityTypeBuilder<OAuthClient> builder)
    {
        builder.ToTable("oauth_clients");

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.ClientId)
            .HasFilter("is_deleted = false")
            .IsUnique();
    }
}
