// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientShiftPreferences;

public record GetAvailableShiftsQuery(Guid ClientId) : IRequest<List<AvailableShiftResource>>;

public class AvailableShiftResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Abbreviation { get; set; }
}
