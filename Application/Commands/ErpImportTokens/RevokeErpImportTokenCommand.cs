// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ErpImportTokens;

public record RevokeErpImportTokenCommand(Guid Id, Guid DropPointId) : IRequest<bool>;
