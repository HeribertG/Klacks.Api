// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Interfaces;

public interface IEmailTestService
{
    Task<EmailTestResult> TestConnectionAsync(EmailTestRequest request);
}
