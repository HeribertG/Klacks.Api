// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class MuteTriggerKindCommand : IRequest<int>
{
    public string UserId { get; set; } = string.Empty;

    public string TriggerKind { get; set; } = string.Empty;

    public bool Muted { get; set; }
}
