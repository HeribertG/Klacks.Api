// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IImapTestService
{
    Task<ImapTestResult> TestConnectionAsync(ImapTestRequest request);
}
