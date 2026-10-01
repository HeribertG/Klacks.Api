// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for ClientImportBatch: soft-delete filter and the unique token index that
/// makes a second commit of the same parsed import fail at the database.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientImportBatchConfiguration : IEntityTypeConfiguration<ClientImportBatch>
{
    public void Configure(EntityTypeBuilder<ClientImportBatch> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Token).IsUnique().HasDatabaseName(ClientImportDatabaseNames.TokenIndex);
    }
}
