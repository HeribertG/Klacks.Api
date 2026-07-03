// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Imports;

public record RetryErpOrderFileCommand(string Key) : IRequest;
