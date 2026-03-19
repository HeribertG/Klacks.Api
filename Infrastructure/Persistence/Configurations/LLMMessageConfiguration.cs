// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die LLMMessage-Entity mit QueryFilter.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMMessageConfiguration : IEntityTypeConfiguration<LLMMessage>
{
    public void Configure(EntityTypeBuilder<LLMMessage> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
