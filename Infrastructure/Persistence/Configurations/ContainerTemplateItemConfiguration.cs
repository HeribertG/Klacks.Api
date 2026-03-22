// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ContainerTemplateItem-Entity with query filter, relationshipen and check constraint.
/// </summary>
/// <param name="builder">Konfiguriert ContainerTemplate-, Shift- und Absence-Beziehungen sowie XOR-Constraint</param>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ContainerTemplateItemConfiguration : IEntityTypeConfiguration<ContainerTemplateItem>
{
    public void Configure(EntityTypeBuilder<ContainerTemplateItem> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(cti => cti.ContainerTemplate)
            .WithMany(ct => ct.ContainerTemplateItems)
            .HasForeignKey(cti => cti.ContainerTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cti => cti.Shift)
            .WithMany()
            .HasForeignKey(cti => cti.ShiftId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cti => cti.Absence)
            .WithMany()
            .HasForeignKey(cti => cti.AbsenceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ContainerTemplateItem_ShiftXorAbsence",
            "(shift_id IS NOT NULL AND absence_id IS NULL) OR (shift_id IS NULL AND absence_id IS NOT NULL)"));
    }
}
