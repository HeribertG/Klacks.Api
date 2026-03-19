// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die Expenses-Entity mit QueryFilter und Work-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ExpensesConfiguration : IEntityTypeConfiguration<Expenses>
{
    public void Configure(EntityTypeBuilder<Expenses> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(e => e.Work)
            .WithMany()
            .HasForeignKey(e => e.WorkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
