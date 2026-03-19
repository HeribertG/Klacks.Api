// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die EmailFolder-Entity mit QueryFilter und Indizes.
/// </summary>
using Klacks.Api.Domain.Models.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EmailFolderConfiguration : IEntityTypeConfiguration<EmailFolder>
{
    public void Configure(EntityTypeBuilder<EmailFolder> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.ImapFolderName).HasFilter("is_deleted = false").IsUnique();
        builder.HasIndex(p => new { p.IsDeleted, p.SortOrder });
    }
}
