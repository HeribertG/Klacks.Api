// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die LLMConversation-Entity mit QueryFilter.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMConversationConfiguration : IEntityTypeConfiguration<LLMConversation>
{
    public void Configure(EntityTypeBuilder<LLMConversation> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
