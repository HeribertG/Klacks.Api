// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Email;

public class ScheduleEmailService : IScheduleEmailService
{
    private readonly DataBaseContext _context;
    private readonly ISettingsEncryptionService? _encryptionService;
    private readonly ILogger<ScheduleEmailService> _logger;

    public ScheduleEmailService(
        DataBaseContext context,
        ISettingsEncryptionService? encryptionService,
        ILogger<ScheduleEmailService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<bool> SendScheduleEmailAsync(string recipientEmail, string clientName,
        string startDate, string endDate, byte[] pdfAttachment, string fileName)
    {
        var msgEmail = new MsgEMail(_context, _logger, _encryptionService);

        var settings = _context.Settings?.ToList();
        var appName = settings?.FirstOrDefault(x => x.Type == Settings.APP_NAME)?.Value ?? "Klacks";

        var subject = BuildSubject(settings, clientName, startDate, endDate, appName);
        var body = BuildBody(settings, clientName, startDate, endDate, appName);

        string? tempFile = null;
        try
        {
            tempFile = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(tempFile, pdfAttachment);

            var email = InitEmailWrapper(settings);
            if (email == null)
            {
                _logger.LogWarning("Email configuration is incomplete, cannot send schedule email to {Email}", recipientEmail);
                return false;
            }

            var result = email.SendEmailMessage(recipientEmail, subject, body, tempFile);
            return result == "true";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send schedule email to {Email}", recipientEmail);
            return false;
        }
        finally
        {
            if (tempFile != null && File.Exists(tempFile))
            {
                try { File.Delete(tempFile); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temp file {TempFile}", tempFile); }
            }
        }
    }

    private static string BuildSubject(List<Domain.Models.Settings.Settings>? settings,
        string clientName, string startDate, string endDate, string appName)
    {
        var template = settings?.FirstOrDefault(x => x.Type == Settings.SCHEDULE_EMAIL_SUBJECT_TEMPLATE)?.Value;

        if (string.IsNullOrWhiteSpace(template))
        {
            template = ScheduleEmailDefaults.Subject;
        }

        return template
            .Replace("{ClientName}", clientName)
            .Replace("{StartDate}", startDate)
            .Replace("{EndDate}", endDate)
            .Replace("{AppName}", appName);
    }

    private static string BuildBody(List<Domain.Models.Settings.Settings>? settings,
        string clientName, string startDate, string endDate, string appName)
    {
        var template = settings?.FirstOrDefault(x => x.Type == Settings.SCHEDULE_EMAIL_BODY_TEMPLATE)?.Value;

        if (string.IsNullOrWhiteSpace(template))
        {
            template = ScheduleEmailDefaults.Body;
        }

        return template
            .Replace("{ClientName}", clientName)
            .Replace("{StartDate}", startDate)
            .Replace("{EndDate}", endDate)
            .Replace("{AppName}", appName);
    }

    private EmailWrapper? InitEmailWrapper(List<Domain.Models.Settings.Settings>? settings)
    {
        if (settings == null || !settings.Any())
        {
            return null;
        }

        var email = new EmailWrapper();

        var tmpEMailAddress = settings.FirstOrDefault(x => x.Type == Settings.APP_EMAIL_ADDRESS)?.Value;
        if (!string.IsNullOrEmpty(tmpEMailAddress))
        {
            email.EMailAddress = tmpEMailAddress;
        }

        email.Mark = settings.FirstOrDefault(x => x.Type == Settings.APP_MARK)?.Value ?? string.Empty;
        email.ReplyTo = settings.FirstOrDefault(x => x.Type == Settings.APP_REPLY_TO)?.Value ?? string.Empty;
        email.Outgoingserver = settings.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER)?.Value ?? string.Empty;
        email.OutgoingserverPort = settings.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_PORT)?.Value ?? string.Empty;
        email.OutgoingserverUsername = settings.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_USERNAME)?.Value ?? string.Empty;

        var encryptedPassword = settings.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_PASSWORD)?.Value ?? string.Empty;
        email.OutgoingserverPassword = DecryptPassword(encryptedPassword);

        var enableSslValue = settings.FirstOrDefault(x => x.Type == Settings.APP_ENABLE_SSL)?.Value;
        email.EnabledSsl = !string.IsNullOrEmpty(enableSslValue) && bool.TryParse(enableSslValue, out var enableSsl) && enableSsl;

        email.AuthenticationType = settings.FirstOrDefault(x => x.Type == Settings.APP_AUTHENTICATION_TYPE)?.Value ?? string.Empty;

        var readReceiptValue = settings.FirstOrDefault(x => x.Type == Settings.APP_READ_RECEIPT)?.Value;
        email.ReadReceipt = !string.IsNullOrEmpty(readReceiptValue) && bool.TryParse(readReceiptValue, out var readReceipt) && readReceipt;

        var dispositionValue = settings.FirstOrDefault(x => x.Type == Settings.APP_DISPOSITION_NOTIFICATION)?.Value;
        email.DispositionNotification = !string.IsNullOrEmpty(dispositionValue) && bool.TryParse(dispositionValue, out var disposition) && disposition;

        var timeoutValue = settings.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_TIMEOUT)?.Value;
        email.OutgoingserverTimeout = !string.IsNullOrEmpty(timeoutValue) && int.TryParse(timeoutValue, out var timeout) ? timeout : 30000;

        return email;
    }

    private string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
        {
            return encryptedPassword;
        }

        if (_encryptionService != null)
        {
            return _encryptionService.ProcessForReading(Settings.APP_OUTGOING_SERVER_PASSWORD, encryptedPassword);
        }

        return encryptedPassword;
    }
}
