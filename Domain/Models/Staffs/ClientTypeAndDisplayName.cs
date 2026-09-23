// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Staffs;

/// <summary>
/// Lean read of a client for inbound-message processing: its type and a human-readable name.
/// </summary>
/// <param name="Type">The client's entity type (employee, extern employee, customer)</param>
/// <param name="DisplayName">"FirstName Name", falling back to Company; empty when the client carries no name at all</param>
public sealed record ClientTypeAndDisplayName(EntityTypeEnum Type, string DisplayName);
