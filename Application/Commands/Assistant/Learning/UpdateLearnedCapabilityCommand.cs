// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant.Learning;

public sealed record UpdateLearnedCapabilityCommand(
    Guid Id,
    string? Goal,
    Dictionary<string, List<string>>? Synonyms) : IRequest<LearningMutationResult>;
