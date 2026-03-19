// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AssignedGroup-Entity mit QueryFilter und Index.
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
