// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Child link between a WizardRunCapture and one Work it created. The deferred measurement job joins on
/// this set to compute post-apply churn: without the created-work references no measurement is possible.
/// </summary>
public class WizardRunCaptureWork : BaseEntity
{
    /// <summary>FK to the parent capture.</summary>
    public Guid CaptureId { get; set; }

    /// <summary>Parent capture navigation.</summary>
    public WizardRunCapture? Capture { get; set; }

    /// <summary>The Work created by the apply.</summary>
    public Guid WorkId { get; set; }
}
