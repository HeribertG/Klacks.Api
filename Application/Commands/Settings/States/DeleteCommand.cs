// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.States;

public record DeleteCommand(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.State>;
