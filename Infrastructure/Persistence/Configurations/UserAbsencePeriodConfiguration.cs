// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the UserAbsencePeriod entity with query filter, lookup index and
/// AppUser relationship. The relationship cascades because AppUser is hard deleted: an absence
/// range of an account that no longer exists carries no information worth keeping.
/// </summary>
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class UserAbsencePeriodConfiguration : IEntityTypeConfiguration<UserAbsencePeriod>
{
    public void Configure(EntityTypeBuilder<UserAbsencePeriod> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.AppUserId)
            .HasColumnName("app_user_id");

        builder.Property(p => p.Reason)
            .HasMaxLength(500);

        builder.HasIndex(p => new { p.AppUserId, p.StartDate, p.EndDate });

        builder.HasOne(p => p.AppUser)
            .WithMany()
            .HasForeignKey(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
