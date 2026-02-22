// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.Branch;

public record PostCommand(Klacks.Api.Domain.Models.Settings.Branch model) : IRequest<Klacks.Api.Domain.Models.Settings.Branch>;
