// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Bots;

public record CreateKlacksBotTokenCommand(string Name, int? ExpiresInDays) : IRequest<KlacksBotTokenCreatedDto>;
