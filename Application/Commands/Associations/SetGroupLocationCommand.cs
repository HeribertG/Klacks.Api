// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Associations;

/// <param name="Id">The group being located</param>
/// <param name="Latitude">Latitude in decimal degrees</param>
/// <param name="Longitude">Longitude in decimal degrees</param>
public record SetGroupLocationCommand(Guid Id, double Latitude, double Longitude) : IRequest<bool>;
