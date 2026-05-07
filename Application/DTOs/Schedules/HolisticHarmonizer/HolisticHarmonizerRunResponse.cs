// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

public sealed record HolisticHarmonizerRunResponse(
    Guid JobId,
    string LlmModelId,
    double FitnessBefore,
    double FitnessAfter,
    IReadOnlyList<HolisticHarmonizerSwapDto> AcceptedSwaps,
    IReadOnlyList<HolisticHarmonizerRejectionDto> RejectedSwaps,
    IReadOnlyList<HolisticHarmonizerBatchDto> Batches,
    string? LlmParsingError,
    string? LlmRawResponsePreview);
