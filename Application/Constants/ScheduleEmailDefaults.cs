// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

public static class ScheduleEmailDefaults
{
    public const string Subject = "Schedule {ClientName} ({StartDate} - {EndDate})";
    public const string Body = "<p>Dear {ClientName},</p><p>Please find enclosed your schedule for {StartDate} - {EndDate}.</p><p>Best regards,<br/>{AppName}</p>";
}
