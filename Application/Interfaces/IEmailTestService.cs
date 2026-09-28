// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Interfaces;

public interface IEmailTestService
{
    Task<EmailTestResult> TestConnectionAsync(EmailTestRequest request);
}
