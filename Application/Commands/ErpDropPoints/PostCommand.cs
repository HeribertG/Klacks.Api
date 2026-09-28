// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ErpDropPoints;

public record PostCommand(ErpDropPointResource Model) : IRequest<ErpDropPointResource?>;
