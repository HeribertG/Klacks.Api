// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupPlaceClassifier
{
    Task<GroupPlaceClassification> ClassifyAsync(
        string groupName,
        IReadOnlyList<string> parentHierarchy,
        CancellationToken cancellationToken = default);
}
