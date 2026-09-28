// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the AssignedGroup entity with query filter and index.
/// </summary>
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AssignedGroupConfiguration : IEntityTypeConfiguration<AssignedGroup>
{
    public void Configure(EntityTypeBuilder<AssignedGroup> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.GroupId });
    }
}
