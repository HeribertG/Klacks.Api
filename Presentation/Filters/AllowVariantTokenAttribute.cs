// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks an action that may be called with the token of a variant (key). ScenarioTokenAccessFilter refuses that token
/// on every other action, admins included. On a marked action it applies to every token the request carries: a variant
/// token passes when the variant's group is visible (IScenarioTokenAccessGuard.IsVariantTokenVisibleAsync), every other
/// token is checked as on any action.
/// </summary>

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowVariantTokenAttribute : Attribute
{
}
