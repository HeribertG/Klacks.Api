// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Mapperly mapper for planning constraints. The write direction only copies the editable fields of
/// <see cref="PlanningConstraintWriteResource"/>; id, origin, approval state, decision trail, version link and
/// import keys are never taken from a request.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class PlanningConstraintMapper
{
    public partial PlanningConstraintResource ToResource(PlanningConstraint constraint);

    public List<PlanningConstraintResource> ToResources(IEnumerable<PlanningConstraint> constraints)
    {
        return constraints.Select(ToResource).ToList();
    }

    [MapperIgnoreTarget(nameof(PlanningConstraint.Id))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.Origin))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ApprovalStatus))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ProposedBy))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ApprovedBy))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ApprovedAt))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.PreviousVersionId))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ImportSourceKey))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ImportContentHash))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CreateTime))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.UpdateTime))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.DeletedTime))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.IsDeleted))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CurrentUserDeleted))]
    public partial PlanningConstraint ToEntity(PlanningConstraintWriteResource resource);

    [MapperIgnoreTarget(nameof(PlanningConstraint.Id))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.Origin))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ApprovalStatus))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ProposedBy))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ApprovedBy))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ApprovedAt))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.PreviousVersionId))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ImportSourceKey))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.ImportContentHash))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.AnalyseToken))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CreateTime))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.UpdateTime))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.DeletedTime))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.IsDeleted))]
    [MapperIgnoreTarget(nameof(PlanningConstraint.CurrentUserDeleted))]
    public partial void ApplyEditableFields(PlanningConstraintWriteResource resource, [MappingTarget] PlanningConstraint target);
}
