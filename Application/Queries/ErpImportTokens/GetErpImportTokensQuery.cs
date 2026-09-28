// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ErpImportTokens;

public record GetErpImportTokensQuery(Guid DropPointId) : IRequest<List<ErpImportTokenListItemDto>>;
