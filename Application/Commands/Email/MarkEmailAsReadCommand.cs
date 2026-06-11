// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Email;

public record MarkEmailAsReadCommand(Guid Id, bool IsRead, string UserId) : IRequest<bool>;
