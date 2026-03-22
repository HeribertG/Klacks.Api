// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    }
}
