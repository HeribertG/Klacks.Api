// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps the settings key-value table. The unique index on Type is what makes a setting a single
/// row: readers resolve a key with FirstOrDefault, and get-or-create callers check for a row and
/// insert when they find none, so without it two concurrent callers -- a second API instance, or
/// an overlapping run -- can each insert a row for the same key and leave which one wins to chance.
/// </summary>
using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SettingsConfiguration : IEntityTypeConfiguration<Klacks.Api.Domain.Models.Settings.Settings>
{
    public void Configure(EntityTypeBuilder<Klacks.Api.Domain.Models.Settings.Settings> builder)
    {
        builder.HasIndex(s => s.Type).IsUnique();
    }
}
