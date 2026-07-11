// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// How many groups in scope match one semantic hypothesis, with a few example names so the
/// model can present a concrete follow-up question instead of a vague one.
/// </summary>
/// <param name="Category">The matched hypothesis (canton, city, qualification, contract, geo or other).</param>
/// <param name="Count">Number of groups in scope matching this category.</param>
/// <param name="ExampleGroupNames">Up to 5 example group names for this category.</param>
public record GroupSemanticCategorySummary(
    GroupSemanticCategory Category,
    int Count,
    IReadOnlyList<string> ExampleGroupNames);
