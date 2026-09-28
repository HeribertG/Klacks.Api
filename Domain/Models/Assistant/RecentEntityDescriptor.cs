// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Immutable result of extracting a trackable entity from a successful skill result: the domain entity
/// type (e.g. "shift", "client"), its persisted id, an optional human-readable display name, and whether
/// the skill created or updated it.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record RecentEntityDescriptor(
    string EntityType,
    Guid EntityId,
    string? DisplayName,
    string Action);
