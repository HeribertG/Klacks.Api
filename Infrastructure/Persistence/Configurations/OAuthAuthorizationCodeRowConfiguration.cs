// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the one-time OAuth authorization codes of the MCP authorization server.
/// The unique index on Code is what makes redemption single-use across instances: consuming deletes
/// the row, and a concurrent second exchange finds nothing left to delete.
/// </summary>

using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class OAuthAuthorizationCodeRowConfiguration : IEntityTypeConfiguration<OAuthAuthorizationCodeRow>
{
    public void Configure(EntityTypeBuilder<OAuthAuthorizationCodeRow> builder)
    {
        builder.ToTable("oauth_authorization_codes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(256);
        builder.Property(p => p.UserId).HasMaxLength(256);
        builder.Property(p => p.ClientId).HasMaxLength(256);
        builder.Property(p => p.ClientName).HasMaxLength(256);
        builder.Property(p => p.RedirectUri).HasMaxLength(2048);
        builder.Property(p => p.CodeChallenge).HasMaxLength(256);
        builder.Property(p => p.Scope).HasMaxLength(1024);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ExpiresAtUtc);
    }
}
