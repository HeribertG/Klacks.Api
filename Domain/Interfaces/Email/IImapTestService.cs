// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IImapTestService
{
    Task<ImapTestResult> TestConnectionAsync(ImapTestRequest request);
}
