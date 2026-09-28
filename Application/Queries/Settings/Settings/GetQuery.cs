// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.Settings;

public record GetQuery(string Type) : IRequest<Klacks.Api.Domain.Models.Settings.Settings?>;
