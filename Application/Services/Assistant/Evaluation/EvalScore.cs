// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public sealed record EvalScore(
    double Composite,
    EvalDimensions Dimensions);
