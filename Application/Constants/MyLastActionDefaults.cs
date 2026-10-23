// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How far back the meta-skills verify_my_last_action and rollback_my_last_change look for the calling user's most
/// recent skill execution. An older execution is not "the last action" of the current conversation any more.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class MyLastActionDefaults
{
    public const int LookbackMinutes = 30;
}
