// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record SelectedClarificationShift(
    DateTime StartUtc,
    string Context);
