// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die ClientContract-Entity mit QueryFilter, Index und Contract-Beziehung.
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
