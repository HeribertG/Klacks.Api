// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

/// <summary>
/// Request to materialise a cached wizard result into the world the wizard ran on.
/// </summary>
/// <param name="JobId">The job whose cached result is materialised.</param>
/// <param name="OverrideBlock">Requests the K1 supervisor override for Block-mode compliance violations.</param>
public sealed record ApplyWizardRequest(Guid JobId, bool OverrideBlock = false);
