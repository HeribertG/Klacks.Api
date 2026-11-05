// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One hourly availability slot of a client, read from and written to the availability endpoints.
/// </summary>
/// <param name="IsAvailable">true = can work, false = blocked, null = remove the stored slot (no entry). Required on write: an
/// explicit null clears, a missing property is refused, so a typo never deletes a block. Reads always carry true or false.</param>

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientAvailabilityResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly Date { get; set; }

    public int Hour { get; set; }

    [JsonRequired]
    public bool? IsAvailable { get; set; }
}
