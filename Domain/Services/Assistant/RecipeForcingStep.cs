// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One ordered step of a recipe forcing plan.
/// </summary>
/// <param name="Skill">The skill name forced for this step.</param>
/// <param name="StepNote">Model-facing guidance appended to the system prompt while this step is forced.</param>
/// <param name="CapturesCustomer">When true, the single matching customer id is captured from this step's result (find_customer_candidates).</param>
/// <param name="NeedsCustomerId">When true, the captured clientId is injected into this step's parameters before execution (create_shift).</param>

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record RecipeForcingStep(
    string Skill,
    string StepNote,
    bool CapturesCustomer = false,
    bool NeedsCustomerId = false);
