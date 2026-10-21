// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of the IMAP email fetch operation.
/// </summary>
/// <param name="Success">Whether the fetch was successful</param>
/// <param name="FetchedCount">Number of newly fetched emails</param>
/// <param name="Error">Error message on failure</param>

namespace Klacks.Api.Application.Commands.Email;

public record FetchEmailsResult(bool Success, int FetchedCount, string? Error = null);
