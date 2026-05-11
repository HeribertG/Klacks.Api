// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public sealed record EvalDimensions(
    double Top1Hit,
    double Top3Hit,
    double AvgLatencyMs,
    int ItemsTotal,
    int ItemsPassed);

public sealed record EvalScore(
    double Composite,
    EvalDimensions Dimensions);
