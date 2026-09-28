// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Schedules;

public class Period : BaseEntity
{
    public Guid IndividualPeriodId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public decimal FullHours { get; set; }

    [JsonIgnore]
    public virtual IndividualPeriod? IndividualPeriod { get; set; }
}
