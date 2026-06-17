// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="AgentDisplayNames">Per-row agent display names in the post-RowSorter
/// bitmap order. Index 0 corresponds to swap.RowA/RowB == 0 in any swap of this run.
/// The frontend uses this array to map row indices to human names without having to
/// reproduce the engine's row sort.</param>
public sealed record HolisticHarmonizerRunResponse(
    Guid JobId,
    string LlmModelId,
    double FitnessBefore,
    double FitnessAfter,
    IReadOnlyList<HolisticHarmonizerSwapDto> AcceptedSwaps,
    IReadOnlyList<HolisticHarmonizerRejectionDto> RejectedSwaps,
    IReadOnlyList<HolisticHarmonizerBatchDto> Batches,
    IReadOnlyList<string> AgentDisplayNames,
    IReadOnlyList<QualificationGapDetail> QualificationGaps,
    string? LlmParsingError,
    string? LlmRawResponsePreview);
