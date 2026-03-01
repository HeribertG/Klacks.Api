// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Staffs;

public class ClientAvailability : BaseEntity
{
    public Guid ClientId { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; }

    public DateOnly Date { get; set; }

    public int Hour { get; set; }

    public bool IsAvailable { get; set; }
}
