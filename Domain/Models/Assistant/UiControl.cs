// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class UiControl : BaseEntity
{
    public string PageKey { get; set; } = string.Empty;

    public string ControlKey { get; set; } = string.Empty;

    public string Selector { get; set; } = string.Empty;

    public string SelectorType { get; set; } = UiControlDefaults.DefaultSelectorType;

    public string ControlType { get; set; } = UiControlDefaults.ControlTypeInput;

    public string? Label { get; set; }

    public string? Route { get; set; }

    public Guid? ParentControlId { get; set; }

    public int SortOrder { get; set; }

    public bool IsDynamic { get; set; }

    public string? SelectorPattern { get; set; }

    public string Metadata { get; set; } = UiControlDefaults.DefaultMetadata;

    public virtual UiControl? ParentControl { get; set; }

    public virtual ICollection<UiControl> ChildControls { get; set; } = new List<UiControl>();
}
