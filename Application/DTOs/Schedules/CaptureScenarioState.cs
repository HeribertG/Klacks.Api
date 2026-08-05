// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record CaptureScenarioState(AnalyseScenarioStatus Status, bool IsDeleted);
