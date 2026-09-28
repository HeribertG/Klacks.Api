// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingPlanPreviewRegistry
{
    void RecordPreview(Guid userId, string fingerprint, Guid? turnId);

    GroupingPreviewStatus GetPreviewStatus(Guid userId, string fingerprint, Guid? currentTurnId);

    void Forget(Guid userId, string fingerprint);
}
