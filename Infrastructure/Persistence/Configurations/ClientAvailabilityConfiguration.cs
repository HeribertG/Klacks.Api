// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ClientAvailability entity with query filter, indexes and Client relationship.
/// </summary>
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientAvailabilityConfiguration : IEntityTypeConfiguration<ClientAvailability>
{
    public void Configure(EntityTypeBuilder<ClientAvailability> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.Date, p.Hour }).IsUnique();
        builder.HasIndex(p => new { p.IsDeleted, p.ClientId, p.Date });

        builder.HasOne(ca => ca.Client)
            .WithMany()
            .HasForeignKey(ca => ca.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
