// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ErpImportException entity: query filter and a lookup index for
/// the open-exceptions admin view.
/// </summary>
using Klacks.Api.Domain.Models.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ErpImportExceptionConfiguration : IEntityTypeConfiguration<ErpImportException>
{
    public void Configure(EntityTypeBuilder<ErpImportException> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => e.ResolvedAt);
    }
}
