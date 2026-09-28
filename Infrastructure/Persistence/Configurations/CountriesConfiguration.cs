// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the Countries-Entity with query filter and MultiLanguage-Name.
/// </summary>
using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CountriesConfiguration : IEntityTypeConfiguration<Countries>
{
    public void Configure(EntityTypeBuilder<Countries> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.ConfigureMultiLanguage(c => c.Name, "name");
    }
}
