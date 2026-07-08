// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the PayrollExportGroupConfig entity with soft-delete query filter and
/// a partial unique index on GroupId (one active config per group).
/// </summary>
using Klacks.Api.Domain.Models.Exports.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PayrollExportGroupConfigConfiguration : IEntityTypeConfiguration<PayrollExportGroupConfig>
{
    public void Configure(EntityTypeBuilder<PayrollExportGroupConfig> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.GroupId)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
