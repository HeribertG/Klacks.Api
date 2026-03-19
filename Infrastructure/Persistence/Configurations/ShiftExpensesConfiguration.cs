// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die ShiftExpenses-Entity mit QueryFilter und Shift-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ShiftExpensesConfiguration : IEntityTypeConfiguration<ShiftExpenses>
{
    public void Configure(EntityTypeBuilder<ShiftExpenses> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(e => e.Shift)
            .WithMany()
            .HasForeignKey(e => e.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
