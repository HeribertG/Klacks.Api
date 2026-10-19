// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the ErpImportFile ledger: pins the table name (the DbSet name would
/// pluralise it), soft-delete query filter, partial unique index on the storage key, the listing and
/// duplicate-lookup indexes, and the relationships to the drop point and to the earlier duplicate.
/// </summary>
using Klacks.Api.Domain.Models.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ErpImportFileConfiguration : IEntityTypeConfiguration<ErpImportFile>
{
    public const string TableName = "erp_import_file";

    private const string NotDeletedFilter = "is_deleted = false";

    public void Configure(EntityTypeBuilder<ErpImportFile> builder)
    {
        builder.ToTable(TableName);

        builder.HasQueryFilter(f => !f.IsDeleted);

        builder.Property(f => f.Sha256).IsFixedLength();

        builder.HasIndex(f => f.StorageKey)
            .IsUnique()
            .HasFilter(NotDeletedFilter);

        builder.HasIndex(f => new { f.DropPointId, f.Sha256 });

        builder.HasIndex(f => new { f.DropPointId, f.Status });

        builder.HasIndex(f => new { f.DropPointId, f.ReceivedAt })
            .IsDescending(false, true);

        builder.HasOne(f => f.DropPoint)
            .WithMany()
            .HasForeignKey(f => f.DropPointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ErpImportFile>()
            .WithMany()
            .HasForeignKey(f => f.DuplicateOfFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
