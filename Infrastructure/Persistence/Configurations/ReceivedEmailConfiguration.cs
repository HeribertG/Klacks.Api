// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die ReceivedEmail-Entity mit QueryFilter und Indizes.
/// </summary>
using Klacks.Api.Domain.Models.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ReceivedEmailConfiguration : IEntityTypeConfiguration<ReceivedEmail>
{
    public void Configure(EntityTypeBuilder<ReceivedEmail> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.MessageId).IsUnique();
        builder.HasIndex(p => new { p.Folder, p.ImapUid });
        builder.HasIndex(p => new { p.SourceImapFolder, p.ImapUid });
        builder.HasIndex(p => new { p.IsDeleted, p.IsRead });
        builder.HasIndex(p => new { p.IsDeleted, p.ReceivedDate });
    }
}
