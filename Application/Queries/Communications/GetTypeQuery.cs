// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Communications;

public record GetTypeQuery() : IRequest<IEnumerable<CommunicationTypeResource>>;
