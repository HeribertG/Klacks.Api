// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces;

public interface IScheduleEmailService
{
    Task<bool> SendScheduleEmailAsync(string recipientEmail, string clientName,
        string startDate, string endDate, byte[] pdfAttachment, string fileName);
}
