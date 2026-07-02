// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ErpImportTokens;

public record CreateErpImportTokenCommand(Guid DropPointId, string Name, int? ExpiresInDays) : IRequest<ErpImportTokenCreatedDto>;
