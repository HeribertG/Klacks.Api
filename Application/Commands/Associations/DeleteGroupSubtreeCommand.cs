// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Associations;

/// <param name="Id">The group whose subtree is removed</param>
public record DeleteGroupSubtreeCommand(Guid Id) : IRequest<DeleteGroupSubtreeResponse>;
