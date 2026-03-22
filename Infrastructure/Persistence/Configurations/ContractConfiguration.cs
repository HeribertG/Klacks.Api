// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Contract-Entity with query filter, Index and relationships to CalendarSelection/SchedulingRule.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.Name, p.ValidFrom, p.ValidUntil });

        builder.HasOne(c => c.CalendarSelection)
            .WithMany()
            .HasForeignKey(c => c.CalendarSelectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.SchedulingRule)
            .WithMany()
            .HasForeignKey(c => c.SchedulingRuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
