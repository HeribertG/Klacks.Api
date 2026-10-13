// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A recipe the message matched but the user must not run because a step skill needs a right the user lacks. Carried out
/// of the recipe match so the turn can tell the model why no guided flow starts, instead of the flow being offered,
/// collecting answers and dying on the first write step.
/// </summary>
/// <param name="RecipeName">Name of the denied recipe</param>
/// <param name="Goal">English goal of the denied recipe, used in the note to the model</param>
/// <param name="MissingRights">The required rights the user does not hold, without duplicates</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record RecipeDenial(string RecipeName, string Goal, IReadOnlyList<string> MissingRights);
