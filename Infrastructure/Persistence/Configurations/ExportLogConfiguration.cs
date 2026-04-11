// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ExportLog entity with query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Exports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ExportLogConfiguration : IEntityTypeConfiguration<ExportLog>
{
    public void Configure(EntityTypeBuilder<ExportLog> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.StartDate, p.EndDate, p.GroupId });
    }
}
