// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ErpImportTokens;

public record GetErpImportTokensQuery(Guid DropPointId) : IRequest<List<ErpImportTokenListItemDto>>;
