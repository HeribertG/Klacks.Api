// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the GlobalAgentRuleHistory-Entity with query filter, Index and Rule-relationship.
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
