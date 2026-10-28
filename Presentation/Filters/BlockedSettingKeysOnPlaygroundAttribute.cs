// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks a settings write action whose body carries one setting (key in Type): on the public Playground the write is
/// answered with HTTP 403 when the key is a speech or translation key or engine (see PlaygroundBlockedSettingKeys),
/// while every other setting still goes through. Without the Playground mode it is a pass-through.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class BlockedSettingKeysOnPlaygroundAttribute : ServiceFilterAttribute
{
    public BlockedSettingKeysOnPlaygroundAttribute()
        : base(typeof(BlockedSettingKeysOnPlaygroundFilter))
    {
    }
}
