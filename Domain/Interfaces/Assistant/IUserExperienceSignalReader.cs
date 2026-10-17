// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IUserExperienceSignalReader
{
    Task<UserExperienceSignals> ReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
