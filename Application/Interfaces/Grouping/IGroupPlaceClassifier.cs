// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupPlaceClassifier
{
    Task<GroupPlaceClassification> ClassifyAsync(
        string groupName,
        IReadOnlyList<string> parentHierarchy,
        CancellationToken cancellationToken = default);
}
