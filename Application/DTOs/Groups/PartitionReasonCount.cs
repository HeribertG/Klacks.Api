// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// How many clients a partition_clients_by_address run could not place for one reason.
/// </summary>
/// <param name="Reason">Why the clients could not be placed.</param>
/// <param name="Count">Number of clients with this reason.</param>
public sealed record PartitionReasonCount(string Reason, int Count);
