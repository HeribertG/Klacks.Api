// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the GroupVisibility-Entity with query filter, column name and AppUser-relationship.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GroupVisibilityConfiguration : IEntityTypeConfiguration<GroupVisibility>
{
    public void Configure(EntityTypeBuilder<GroupVisibility> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(e => e.AppUserId)
            .HasColumnName("app_user_id");

        builder.HasIndex(p => new { p.AppUserId, p.GroupId });

        builder.HasOne(p => p.AppUser)
            .WithMany()
            .HasForeignKey(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
