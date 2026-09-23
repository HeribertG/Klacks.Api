// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answer deadline of a clarification question: DefaultAnswerWindowMinutes after asking, but at the
/// latest ShiftStartBufferMinutes before the affected shift starts, and never less than
/// MinimumAnswerWindowMinutes after asking (a shift starting in a few minutes, or already running,
/// would otherwise expire the question at once). All values are UTC.
/// </summary>
/// <param name="askedAtUtc">When the question was sent</param>
/// <param name="shiftStartUtc">Start of the affected shift, when one is known</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Inbound;

public static class ClarificationDeadlineCalculator
{
    public static DateTime Compute(DateTime askedAtUtc, DateTime? shiftStartUtc)
    {
        var asked = DateTime.SpecifyKind(askedAtUtc, DateTimeKind.Utc);
        var deadline = asked.AddMinutes(InboundClarificationConstants.DefaultAnswerWindowMinutes);

        if (shiftStartUtc is { } shiftStart)
        {
            var beforeShift = DateTime.SpecifyKind(shiftStart, DateTimeKind.Utc)
                .AddMinutes(-InboundClarificationConstants.ShiftStartBufferMinutes);
            if (beforeShift < deadline)
            {
                deadline = beforeShift;
            }
        }

        var earliest = asked.AddMinutes(InboundClarificationConstants.MinimumAnswerWindowMinutes);
        return deadline < earliest ? earliest : deadline;
    }
}
