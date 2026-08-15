// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the one-time OAuth2 state rows. The unique index on State is what makes
/// the value single-use across instances: consumption deletes the row and a concurrent second
/// callback finds nothing left to delete.
/// </summary>

using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class OAuth2StateRowConfiguration : IEntityTypeConfiguration<OAuth2StateRow>
{
    public void Configure(EntityTypeBuilder<OAuth2StateRow> builder)
    {
        builder.ToTable("oauth2_states");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.State).HasMaxLength(128);
        builder.HasIndex(p => p.State).IsUnique();
        builder.HasIndex(p => p.ExpiresAtUtc);
    }
}
