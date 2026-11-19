// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF configuration of the directory link on AppUser: a directory entry links to at most one account per provider
/// (unique partial index over provider and external id). The foreign key nulls the link only on a hard delete of the
/// provider row; the usual soft delete keeps it, and SetDirectoryAccountAccessCommandHandler moves such a link to the
/// provider an administrator grants the entry on next.
/// </summary>

using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    private const string LinkedFilterSql = "\"identity_provider_id\" IS NOT NULL";

    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasIndex(u => new { u.IdentityProviderId, u.DirectoryExternalId })
            .IsUnique()
            .HasFilter(LinkedFilterSql);

        builder.HasOne<IdentityProvider>()
            .WithMany()
            .HasForeignKey(u => u.IdentityProviderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
