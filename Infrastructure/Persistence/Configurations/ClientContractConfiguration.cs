// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ClientContract-Entity with query filter, Index and Contract-relationship.
/// </summary>
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientContractConfiguration : IEntityTypeConfiguration<ClientContract>
{
    public void Configure(EntityTypeBuilder<ClientContract> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.ClientId, p.ContractId, p.FromDate, p.UntilDate });

        builder.HasOne(cc => cc.Contract)
            .WithMany()
            .HasForeignKey(cc => cc.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
