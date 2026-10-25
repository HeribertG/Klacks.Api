// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One ordered step of a recipe forcing plan.
/// </summary>
/// <param name="Skill">The skill name forced for this step.</param>
/// <param name="StepNote">Model-facing guidance appended to the system prompt while this step is forced.</param>
/// <param name="CapturesCustomer">When true, the single matching customer id is captured from this step's result (find_customer_candidates).</param>
/// <param name="NeedsCustomerId">When true, the captured clientId is injected into this step's parameters before execution (create_shift).</param>
/// <param name="CapturesOrder">When true, the id of the order this step created is captured from its result (create_shift, Data.ShiftId).</param>
/// <param name="NeedsOrderId">When true, the captured order id is injected as shiftId before execution (seal_shift).</param>
/// <param name="SealsOrder">When true, the step is the seal: a held call keeps the plan waiting for the user's confirmation, a
/// refused call ends the plan, and the call that ran (directly or as the confirmed replay) yields the plannable shift id (seal_shift).</param>
/// <param name="NeedsPlannableShiftId">When true, the captured plannable shift id is injected as shiftId before execution (cut_shift).</param>

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record RecipeForcingStep(
    string Skill,
    string StepNote,
    bool CapturesCustomer = false,
    bool NeedsCustomerId = false,
    bool CapturesOrder = false,
    bool NeedsOrderId = false,
    bool SealsOrder = false,
    bool NeedsPlannableShiftId = false);
