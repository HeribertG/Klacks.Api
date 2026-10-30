// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.Services.Update;

/// <summary>
/// The operations that replace Klacks itself. The update status and the update advice both read the newest of these, so
/// a third-party operation in the same history never counts as the last Klacks update.
/// </summary>
public static class KlacksSelfUpdateTypes
{
    public static readonly UpdateOperationType[] All = [UpdateOperationType.Update, UpdateOperationType.Rollback];
}
