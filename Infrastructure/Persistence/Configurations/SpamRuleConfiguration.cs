// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die SpamRule-Entity mit QueryFilter und Index.
/// </summary>
using Klacks.Api.Domain.Models.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SpamRuleConfiguration : IEntityTypeConfiguration<SpamRule>
{
    public void Configure(EntityTypeBuilder<SpamRule> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.IsActive, p.SortOrder });
    }
}
