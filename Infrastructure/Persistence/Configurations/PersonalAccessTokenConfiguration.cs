// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the PersonalAccessToken entity with query filter,
/// unique partial token hash index, user index and AppUser relationship.
/// </summary>
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PersonalAccessTokenConfiguration : IEntityTypeConfiguration<PersonalAccessToken>
{
    public void Configure(EntityTypeBuilder<PersonalAccessToken> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => p.TokenHash)
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
