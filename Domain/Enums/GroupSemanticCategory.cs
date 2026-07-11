// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// A hypothesis about the semantic criterion behind a group's name, used by
/// analyze_group_semantics to help the model ask an informed follow-up question
/// instead of guessing how the user wants clients sorted into groups.
/// </summary>
public enum GroupSemanticCategory
{
    Canton,
    City,
    Qualification,
    Contract,
    Geo,
    Other
}
