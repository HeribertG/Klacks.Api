// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that turns a bulk Work deletion plan into the facts a confirmation is bound to: the ids of the planned
/// Works (kind Work, "N" format). The preview provider stores them with the token and the skill compares them with its
/// own fresh plan right before it deletes, so both sides always form the items the same way.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public static class BulkWorkDeletionBinding
{
    private const string WorkIdFormat = "N";

    public static ConfirmationBinding Of(BulkWorkDeletionPlan plan) =>
        ConfirmationBinding.Of(
            ConfirmationBindingKinds.Work,
            plan.Works.Select(work => work.WorkId.ToString(WorkIdFormat)));
}
