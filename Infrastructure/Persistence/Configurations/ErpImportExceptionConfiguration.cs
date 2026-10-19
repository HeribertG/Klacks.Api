// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the ErpImportException entity: query filter, a lookup index for the
/// open-exceptions admin view and the optional link to the ledger row of the file the order came from.
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

        builder.HasIndex(e => e.ImportFileId);

        builder.HasOne<ErpImportFile>()
            .WithMany()
            .HasForeignKey(e => e.ImportFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
