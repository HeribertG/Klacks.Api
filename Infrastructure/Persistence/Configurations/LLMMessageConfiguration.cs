// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the LLMMessage-Entity with query filter.
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
