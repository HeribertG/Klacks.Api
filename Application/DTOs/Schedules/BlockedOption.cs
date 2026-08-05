// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A repair option the compliance guardrail refused as a whole.
/// </summary>
/// <param name="Index">Position of the option in the submitted list.</param>
/// <param name="ReasonKey">First blocking error reported for it.</param>
public sealed record BlockedOption(int Index, string ReasonKey);
