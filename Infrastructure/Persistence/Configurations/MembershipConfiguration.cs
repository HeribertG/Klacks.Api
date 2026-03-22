// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Membership-Entity with query filter, Index and Client-relationship.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.ValidFrom, p.ValidUntil, p.IsDeleted });

        builder.HasOne(m => m.Client)
            .WithOne(c => c.Membership)
            .HasForeignKey<Membership>(m => m.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
