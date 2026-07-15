// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Qualification master entity with soft-delete query filter.
/// ImportSourceKey is unique among ACTIVE IMPORTED rows only — customer-created qualifications all
/// carry the empty string, so the partial index must exclude it.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class QualificationConfiguration : IEntityTypeConfiguration<Qualification>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<Qualification> builder)
    {
        builder.HasQueryFilter(q => !q.IsDeleted);
        builder.ConfigureMultiLanguage(q => q.Name, "name");
        builder.ConfigureMultiLanguage(q => q.Description, "description");
        builder.Property(q => q.Type).HasDefaultValue(QualificationType.Work);
        builder.Property(q => q.Category).HasDefaultValue(QualificationCategory.None);

        builder.Property(q => q.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(q => q.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength).HasDefaultValue(string.Empty);

        builder.HasIndex(q => q.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
