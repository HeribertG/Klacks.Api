// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// What an external agent may do with a personal access token. Read is deliberately 0, so an
/// unset value is the safe one.
/// </summary>
public enum PersonalAccessTokenAccessMode
{
    Read = 0,
    Write = 1
}
