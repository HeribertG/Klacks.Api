// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the ShiftDayAssignment-Entity as keyless entity.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ShiftDayAssignmentConfiguration : IEntityTypeConfiguration<ShiftDayAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftDayAssignment> builder)
    {
        builder.HasNoKey();
        builder.Ignore(x => x.RequiredQualifications);
    }
}
