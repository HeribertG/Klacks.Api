// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class GroupItem : BaseEntity
{
    [ForeignKey("Employee")]
    public Guid? ClientId { get; set; }

    [ForeignKey("Shift")]
    public Guid? ShiftId { get; set; }

    [Required]
    [ForeignKey("Group")]
    public Guid GroupId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    public Guid? AnalyseToken { get; set; }

    public Guid? ScenarioSourceGroupItemId { get; set; }

    /// <summary>
    /// True for a membership a cover lent a person for single days (a borrowed stand-in), in a scenario and after the
    /// scenario is taken over. Such a row counts in the schedule on its days, but is no regular membership of the master data.
    /// </summary>
    public bool IsTemporary { get; set; }

    [JsonIgnore]
    public virtual Client? Client { get; set; } = null!;

    [JsonIgnore]
    public virtual Shift? Shift { get; set; } = null!;

    [JsonIgnore]
    public virtual Group? Group { get; set; }
}
