// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Mediator;

public interface IRequest<out TResponse>;

public interface IRequest : IRequest<Unit>;
