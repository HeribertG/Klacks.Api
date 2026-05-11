// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class SubmitCorrectionCommand : IRequest<SubmitCorrectionResult>
{
    public string UserId { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string CorrectionType { get; set; } = string.Empty;
    public string? ExpectedSkill { get; set; }
}

public sealed record SubmitCorrectionResult(bool Found, Guid? TrajectoryId);
