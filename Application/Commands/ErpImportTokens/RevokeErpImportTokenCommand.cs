// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ErpImportTokens;

public record RevokeErpImportTokenCommand(Guid Id, Guid DropPointId) : IRequest<bool>;
