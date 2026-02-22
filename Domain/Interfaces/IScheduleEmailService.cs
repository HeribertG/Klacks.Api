// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces;

public interface IScheduleEmailService
{
    Task<bool> SendScheduleEmailAsync(string recipientEmail, string clientName,
        string startDate, string endDate, byte[] pdfAttachment, string fileName);
}
