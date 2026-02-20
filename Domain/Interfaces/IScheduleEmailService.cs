namespace Klacks.Api.Domain.Interfaces;

public interface IScheduleEmailService
{
    Task<string> SendScheduleEmailAsync(string recipientEmail, string clientName,
        string startDate, string endDate, byte[] pdfAttachment, string fileName);
}
