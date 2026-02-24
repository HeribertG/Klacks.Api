// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.CalendarSelections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Associations;

public class Group : BaseEntity
{
    public Group()
    {
        GroupItems = new Collection<GroupItem>();
    }

    public string Description { get; set; } = string.Empty;

    public ICollection<GroupItem> GroupItems { get; set; }

    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    public PaymentInterval PaymentInterval { get; set; } = PaymentInterval.Monthly;

    [ForeignKey("CalendarSelection")]
    public Guid? CalendarSelectionId { get; set; }

    [JsonIgnore]
    public CalendarSelection? CalendarSelection { get; set; }

    public Guid? Parent { get; set; }

    public Guid? Root { get; set; }

    public int Lft { get; set; }

    public int Rgt { get; set; }
}
