// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ClientShiftPreferences;

public record SaveClientShiftPreferencesCommand(
    Guid ClientId,
    List<ClientShiftPreferenceResource> Preferences
) : IRequest<List<ClientShiftPreferenceResource>>;
