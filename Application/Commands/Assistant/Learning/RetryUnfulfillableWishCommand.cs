// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant.Learning;

public sealed record RetryUnfulfillableWishCommand(Guid Id) : IRequest<LearningMutationResult>;
