// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Qualification master entity with soft-delete query filter.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.HasQueryFilter(q => !q.IsDeleted);
        builder.ConfigureMultiLanguage(q => q.Name, "name");
        builder.ConfigureMultiLanguage(q => q.Description, "description");
        builder.Property(q => q.Type).HasDefaultValue(QualificationType.Work);
        builder.Property(q => q.Category).HasDefaultValue(QualificationCategory.None);
    }
}
