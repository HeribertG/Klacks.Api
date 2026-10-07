// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <summary>
/// Records a contact attempt with an alternative candidate for one slot (upsert on scenario token, candidate,
/// shift and date). Only the fact is stored; the proposal in the scenario is not changed.
/// </summary>
/// <param name="Request">Slot, candidate, context and the recorded answer</param>
public record RecordReplacementContactCommand(RecordReplacementContactRequest Request)
    : IRequest<ReplacementRequestResource>;
