// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the ExportLogItem entity: soft-delete query filter, jsonb snapshot, the foreign key to
/// the export run and a partial unique index on the revision of a person, period and format, which makes two parallel
/// exports compute the same next revision and collide instead of both succeeding. Its leading columns (person,
/// period) also serve the overlap and latest-revision lookups.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Exports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ExportLogItemConfiguration : IEntityTypeConfiguration<ExportLogItem>
{
    private const string NotDeletedFilter = "\"is_deleted\" = false";
    private const string JsonbColumnType = "jsonb";
    private static readonly string CharColumnType = $"char({ExportLogLimits.ContentHashLength})";

    public void Configure(EntityTypeBuilder<ExportLogItem> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.EntriesJson).HasColumnType(JsonbColumnType);
        builder.Property(p => p.ContentHash).HasColumnType(CharColumnType);

        builder.HasOne<ExportLog>()
            .WithMany()
            .HasForeignKey(p => p.ExportLogId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ClientId, p.StartDate, p.EndDate, p.Format, p.Revision })
            .IsUnique()
            .HasFilter(NotDeletedFilter);
        builder.HasIndex(p => p.ExportLogId);
    }
}
