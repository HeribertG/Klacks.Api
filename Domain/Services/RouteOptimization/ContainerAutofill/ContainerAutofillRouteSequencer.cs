// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Phase 2 of the container autofill: reorders the greedy selection with the ant colony optimizer (incl. 2-opt) on
/// a sub matrix of the bases and the selected stops. Up to two stops are returned unchanged (same list instance).
/// </summary>
/// <param name="logger">Handed to the ant colony optimizer, which logs its progress</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillRouteSequencer
{
    private const int MaxStopsWithoutOptimization = 2;

    private readonly ILogger<ContainerAutofillRouteSequencer> _logger;

    public ContainerAutofillRouteSequencer(ILogger<ContainerAutofillRouteSequencer> logger)
    {
        _logger = logger;
    }

    public List<int> OptimizeSelectedRoute(
        DistanceMatrix distanceMatrix,
        List<int> selectedIndices,
        int startBaseIndex,
        int endBaseIndex)
    {
        if (selectedIndices.Count <= MaxStopsWithoutOptimization)
        {
            return selectedIndices;
        }

        var indexMap = BuildIndexMap(selectedIndices, startBaseIndex, endBaseIndex);
        var subMatrix = BuildSubMatrix(distanceMatrix, indexMap);
        var optimizer = new AntColonyOptimizer(subMatrix, _logger);

        int subStartIndex = 0;
        int? subEndIndex = endBaseIndex != startBaseIndex ? indexMap.Count - 1 : 0;
        var optimizedSubRoute = optimizer.FindOptimalRoute(subStartIndex, subEndIndex);

        var optimizedOriginalIndices = new List<int>();
        foreach (var subIdx in optimizedSubRoute)
        {
            var originalIdx = indexMap[subIdx];
            if (originalIdx != startBaseIndex && originalIdx != endBaseIndex)
            {
                optimizedOriginalIndices.Add(originalIdx);
            }
        }

        return optimizedOriginalIndices;
    }

    private static List<int> BuildIndexMap(List<int> selectedIndices, int startBaseIndex, int endBaseIndex)
    {
        var indexMap = new List<int> { startBaseIndex };
        indexMap.AddRange(selectedIndices);
        if (endBaseIndex != startBaseIndex)
        {
            indexMap.Add(endBaseIndex);
        }

        return indexMap;
    }

    private static DistanceMatrix BuildSubMatrix(DistanceMatrix distanceMatrix, List<int> indexMap)
    {
        var subLocations = indexMap.Select(idx => distanceMatrix.Locations[idx]).ToList();
        var subSize = subLocations.Count;
        var subDistanceMatrix = new double[subSize, subSize];
        var subDurationMatrix = new double[subSize, subSize];

        for (int i = 0; i < subSize; i++)
        {
            for (int j = 0; j < subSize; j++)
            {
                subDistanceMatrix[i, j] = distanceMatrix.Matrix[indexMap[i], indexMap[j]];
                subDurationMatrix[i, j] = distanceMatrix.DurationMatrix[indexMap[i], indexMap[j]];
            }
        }

        return new DistanceMatrix(subLocations, subDistanceMatrix, subDurationMatrix, distanceMatrix.DurationMatricesByProfile, distanceMatrix.IsEstimated);
    }
}
