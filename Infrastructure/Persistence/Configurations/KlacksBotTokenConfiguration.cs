// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the KlacksBotToken entity: soft-delete query filter and the
/// unique hash index used for authentication lookups.
/// </summary>
using Klacks.Api.Domain.Models.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class KlacksBotTokenConfiguration : IEntityTypeConfiguration<KlacksBotToken>
{
    public void Configure(EntityTypeBuilder<KlacksBotToken> builder)
    {
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
