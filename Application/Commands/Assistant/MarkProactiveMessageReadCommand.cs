// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class MarkProactiveMessageReadCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;
}
