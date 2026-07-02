// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ErpImportToken entity: query filter, unique hash index and
/// the relationship to its owning ErpDropPoint.
/// </summary>
using Klacks.Api.Domain.Models.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ErpImportTokenConfiguration : IEntityTypeConfiguration<ErpImportToken>
{
    public void Configure(EntityTypeBuilder<ErpImportToken> builder)
    {
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(t => t.DropPoint)
            .WithMany()
            .HasForeignKey(t => t.DropPointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
