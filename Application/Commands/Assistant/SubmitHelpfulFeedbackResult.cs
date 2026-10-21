// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Commands.Assistant;

public sealed record SubmitHelpfulFeedbackResult(bool Found, Guid? TrajectoryId);
