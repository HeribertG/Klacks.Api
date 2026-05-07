// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.Wizard;

public sealed record ApplyWizardResponse(IReadOnlyList<Guid> CreatedWorkIds);
