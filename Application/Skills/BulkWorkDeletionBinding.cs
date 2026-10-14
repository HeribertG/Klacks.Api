// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that turns a bulk Work deletion plan into the facts a confirmation is bound to (kind Work, "N" format): the
/// ids of the planned Works, and the ids of the container children (child Works and Breaks) the delete takes along with
/// them, each under its own prefix so a child never collides with a parent or with a child of the other kind. The preview
/// provider stores them with the token and the skill compares them with its own fresh plan right before it deletes, so both
/// sides always form the items the same way. A plan without container children binds like a plain list of work ids.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public static class BulkWorkDeletionBinding
{
    private const string IdFormat = "N";
    private const string ChildWorkPrefix = "child-work:";
    private const string ChildBreakPrefix = "child-break:";

    public static ConfirmationBinding Of(BulkWorkDeletionPlan plan) =>
        ConfirmationBinding.Of(
            ConfirmationBindingKinds.Work,
            plan.Works.Select(work => work.WorkId.ToString(IdFormat))
                .Concat(plan.ChildWorkIds.Select(id => ChildWorkPrefix + id.ToString(IdFormat)))
                .Concat(plan.ChildBreakIds.Select(id => ChildBreakPrefix + id.ToString(IdFormat))));
}
