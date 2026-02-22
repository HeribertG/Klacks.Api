// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Mediator;

public interface IRequest<out TResponse>;

public interface IRequest : IRequest<Unit>;
