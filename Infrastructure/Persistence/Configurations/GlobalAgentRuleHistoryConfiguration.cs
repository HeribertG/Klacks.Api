// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die GlobalAgentRuleHistory-Entity mit QueryFilter, Index und Rule-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GlobalAgentRuleHistoryConfiguration : IEntityTypeConfiguration<GlobalAgentRuleHistory>
{
    public void Configure(EntityTypeBuilder<GlobalAgentRuleHistory> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.GlobalAgentRuleId, p.CreateTime });

        builder.HasOne(h => h.GlobalAgentRule)
            .WithMany(r => r.History)
            .HasForeignKey(h => h.GlobalAgentRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
