// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the LLMModel-Entity with query filter.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMModelConfiguration : IEntityTypeConfiguration<LLMModel>
{
    public void Configure(EntityTypeBuilder<LLMModel> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
