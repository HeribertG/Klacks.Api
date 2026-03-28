// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientShiftPreferences;

public record ListByClientQuery(Guid ClientId) : IRequest<List<ClientShiftPreferenceResource>>;
