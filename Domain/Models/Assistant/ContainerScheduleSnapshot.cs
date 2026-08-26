// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The part of a container shift's own definition a template remediation needs, captured by the
/// detector at detection time and carried in the condition's payload. It exists because
/// IConditionRemediationParameterBinder is contractually a PURE function over that payload - no
/// repository, no I/O - so anything the binder has to know must already be in the payload when the
/// ledger row is opened. Reading the Shift again at remediation time would also be wrong on its own
/// terms: it would silently bind against a definition nobody has looked at since the finding was
/// reported.
/// </summary>
/// <param name="StartShift">The container's own start of day, the template's fromTime.</param>
/// <param name="EndShift">The container's own end of day, the template's untilTime.</param>
/// <param name="IsoWeekdays">Ascending ISO weekday numbers (1 = Monday .. 7 = Sunday) the container runs on; empty when it carries no weekday at all.</param>
/// <param name="IsHoliday">The container's holiday-only flag, mirrored onto the template.</param>
/// <param name="IsWeekdayAndHoliday">The container's weekday-and-holiday flag, mirrored onto the template.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ContainerScheduleSnapshot(
    TimeOnly StartShift,
    TimeOnly EndShift,
    IReadOnlyCollection<int> IsoWeekdays,
    bool IsHoliday,
    bool IsWeekdayAndHoliday);
