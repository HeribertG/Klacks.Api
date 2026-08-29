// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A user marking one chat answer as helpful. The positive counterpart of SubmitCorrectionCommand and
/// the only positive signal the usefulness oracle has: without it a learned artefact could only ever be
/// measured by what went wrong.
/// </summary>
/// <param name="UserId">Identity of the person judging, taken from the token, never from the body</param>
/// <param name="UserMessage">The utterance the answer responded to; hashed to find the turn</param>
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class SubmitHelpfulFeedbackCommand : IRequest<SubmitHelpfulFeedbackResult>
{
    public string UserId { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;
}

public sealed record SubmitHelpfulFeedbackResult(bool Found, Guid? TrajectoryId);
