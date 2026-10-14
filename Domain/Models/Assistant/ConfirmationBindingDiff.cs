// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How the facts a confirmation was bound to differ from the facts that apply now.
/// </summary>
/// <param name="ShownCount">Number of items the user was shown</param>
/// <param name="CurrentCount">Number of items that apply now</param>
/// <param name="AddedCount">Items that apply now but were not shown</param>
/// <param name="GoneCount">Items that were shown but no longer apply</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConfirmationBindingDiff(int ShownCount, int CurrentCount, int AddedCount, int GoneCount);
