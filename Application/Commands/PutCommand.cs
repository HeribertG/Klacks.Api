// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands;

public record PutCommand<TModel>(TModel Resource) : IRequest<TModel?>;
