// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ClientAvailabilityScheduleEntry entity as keyless view entity.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientAvailabilityScheduleEntryConfiguration : IEntityTypeConfiguration<ClientAvailabilityScheduleEntry>
{
    public void Configure(EntityTypeBuilder<ClientAvailabilityScheduleEntry> builder)
    {
        builder.HasNoKey().ToView(null);
    }
}
