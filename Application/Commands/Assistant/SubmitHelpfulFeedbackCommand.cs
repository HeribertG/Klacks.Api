// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A user marking one chat answer as helpful or not helpful (W1.8). The thumbs-up is the positive
/// counterpart of SubmitCorrectionCommand and the only positive signal the usefulness oracle has;
/// the thumbs-down feeds an explicit negative case into the learning loop.
/// </summary>
/// <param name="UserId">Identity of the person judging, taken from the token, never from the body</param>
/// <param name="UserMessage">The utterance the answer responded to; hashed to find the turn</param>
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class SubmitHelpfulFeedbackCommand : IRequest<SubmitHelpfulFeedbackResult>
{
    public string UserId { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// Null means thumbs-up (the historical meaning of this endpoint, W1.8 keeps that backward
    /// compatible). False is the thumbs-down: the trajectory is marked not helpful and the learning
    /// loop receives an explicit negative case.
    /// </summary>
    public bool? Helpful { get; set; }

    /// <summary>
    /// Optional free-text the user attached to a thumbs-down. Truncated to
    /// SkillLearningDefaults.FeedbackCommentMaxLength, never rejected.
    /// </summary>
    public string? Comment { get; set; }
}

public sealed record SubmitHelpfulFeedbackResult(bool Found, Guid? TrajectoryId);
