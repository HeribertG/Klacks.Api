// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ClientSortPreference: soft-delete filter, partial unique index, sort index.
/// </summary>

using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientSortPreferenceConfiguration : IEntityTypeConfiguration<ClientSortPreference>
{
    public void Configure(EntityTypeBuilder<ClientSortPreference> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(e => e.UserId).IsRequired();

        builder.HasIndex(p => new { p.UserId, p.GroupId, p.ClientId })
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(p => new { p.UserId, p.GroupId, p.SortOrder });
    }
}
