// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.States;

public record PutCommand(Klacks.Api.Domain.Models.Settings.State model) : IRequest<Klacks.Api.Domain.Models.Settings.State>;
