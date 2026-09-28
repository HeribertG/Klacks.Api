// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface ISpamFilterService
{
    Task<SpamFilterResult> ClassifyAsync(ReceivedEmail email, CancellationToken cancellationToken);
}
