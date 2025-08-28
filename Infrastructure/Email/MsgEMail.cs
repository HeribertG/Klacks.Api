using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Email;

public class MsgEMail
{
    private readonly DataBaseContext context;

    public MsgEMail(DataBaseContext context)
    {
        this.context = context;
    }

    public string SendMail(string currenteMail, string subject, string msg)
    {
        var email = InitEMailWrapper();
        if (email != null)
        {
            return email.SendEmailMessage(currenteMail, subject, msg, string.Empty);
        }

        return "Wrong Initialisation of the Email Wrapper";
    }

    private EmailWrapper? InitEMailWrapper()
    {
        try
        {
            // Use explicit ToList() call to avoid deferred execution issues
            var setting = context.Settings?.ToList();

            if (setting == null || !setting.Any())
            {
                // No email settings configured
                return null;
            }

            var email = new EmailWrapper();

            var tmpEMailAddress = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_EMAIL_ADDRESS)?.Value;
            if (!string.IsNullOrEmpty(tmpEMailAddress))
            {
                email.EMailAddress = tmpEMailAddress;
            }

            var tmpSubject = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_SUBJECT)?.Value;
            if (!string.IsNullOrEmpty(tmpSubject))
            {
                email.Subject = tmpSubject;
            }

            email.Mark = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_MARK)?.Value ?? string.Empty;
            email.ReplyTo = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_REPLY_TO)?.Value ?? string.Empty;
            email.Outgoingserver = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_OUTGOING_SERVER)?.Value ?? string.Empty;
            email.OutgoingserverPort = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_OUTGOING_SERVER_PORT)?.Value ?? string.Empty;
            email.OutgoingserverUsername = setting.FirstOrDefault(x => x.Type == "outgoingserverUsername")?.Value ?? string.Empty;
            email.OutgoingserverPassword = setting.FirstOrDefault(x => x.Type == "outgoingserverPassword")?.Value ?? string.Empty;

            // Handle boolean parsing more safely
            var enableSslValue = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_ENABLE_SSL)?.Value;
            email.EnabledSsl = !string.IsNullOrEmpty(enableSslValue) && bool.TryParse(enableSslValue, out var enableSsl) && enableSsl;

            email.AuthenticationType = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_AUTHENTICATION_TYPE)?.Value ?? string.Empty;

            var readReceiptValue = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_READ_RECEIPT)?.Value;
            email.ReadReceipt = !string.IsNullOrEmpty(readReceiptValue) && bool.TryParse(readReceiptValue, out var readReceipt) && readReceipt;

            var dispositionValue = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_DISPOSITION_NOTIFICATION)?.Value;
            email.DispositionNotification = !string.IsNullOrEmpty(dispositionValue) && bool.TryParse(dispositionValue, out var disposition) && disposition;

            var timeoutValue = setting.FirstOrDefault(x => x.Type == Application.Constants.Settings.APP_OUTGOING_SERVER_TIMEOUT)?.Value;
            email.OutgoingserverTimeout = !string.IsNullOrEmpty(timeoutValue) && int.TryParse(timeoutValue, out var timeout) ? timeout : 30000; // Default 30 seconds

            return email;
        }
        catch (Exception ex)
        {
            // Log the specific error for debugging
            Console.WriteLine($"Email configuration error: {ex.Message}");
            return null;
        }
    }
}
