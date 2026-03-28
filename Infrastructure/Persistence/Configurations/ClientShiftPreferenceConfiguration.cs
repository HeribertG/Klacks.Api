// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ClientShiftPreference with query filter, indexes and relationships.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientShiftPreferenceConfiguration : IEntityTypeConfiguration<ClientShiftPreference>
{
    public void Configure(EntityTypeBuilder<ClientShiftPreference> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.IsDeleted });
        builder.HasIndex(p => new { p.ClientId, p.ShiftId, p.PreferenceType });

        builder.HasOne(csp => csp.Shift)
            .WithMany()
            .HasForeignKey(csp => csp.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(csp => csp.Client)
            .WithMany(c => c.ShiftPreferences)
            .HasForeignKey(csp => csp.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
