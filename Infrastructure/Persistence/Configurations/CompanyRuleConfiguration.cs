// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the CompanyRule registry entity: soft-delete query filter, bounded-length
/// Name/TargetEntityType columns and an index over the target so rules applied to a given entity are
/// looked up efficiently.
/// </summary>

using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CompanyRuleConfiguration : IEntityTypeConfiguration<CompanyRule>
{
    private const int NameMaxLength = 200;
    private const int TargetEntityTypeMaxLength = 50;

    public void Configure(EntityTypeBuilder<CompanyRule> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(NameMaxLength);
        builder.Property(p => p.TargetEntityType).IsRequired().HasMaxLength(TargetEntityTypeMaxLength);
        builder.Property(p => p.AppliedParametersJson).IsRequired().HasDefaultValue(string.Empty);

        builder.HasIndex(p => new { p.IsDeleted, p.TargetEntityType, p.TargetEntityId });
    }
}
