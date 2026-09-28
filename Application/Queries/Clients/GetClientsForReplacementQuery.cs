// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.Api.Application.Queries.Clients;

public sealed record GetClientsForReplacementQuery() : IRequest<IEnumerable<ClientForReplacementResource>>;
