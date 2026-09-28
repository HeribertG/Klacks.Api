// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Shared formatting patterns for proactive operational alerts. Centralised so the displayed date
/// format stays consistent across all trigger events.
/// </summary>
public static class ProactiveMessageFormats
{
    public const string DisplayDate = "dd.MM.yyyy";
    public const string ActionDate = "yyyy-MM-dd";
}
