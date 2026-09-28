// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Interfaces.Update;

public interface IUpdateAvailabilityEvaluator
{
    UpdateAvailability Evaluate(SemanticVersion currentVersion, UpdateManifest manifest);
}
