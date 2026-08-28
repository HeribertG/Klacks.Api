// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant.Learning;

public sealed record UpdateLearnedPhraseCommand(Guid Id, string? Phrase, string? Description)
    : IRequest<LearningMutationResult>;
