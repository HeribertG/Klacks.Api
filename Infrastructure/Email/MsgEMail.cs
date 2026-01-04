using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Email;

public class MsgEMail
{
    private readonly DataBaseContext context;
    private readonly ISettingsEncryptionService? _encryptionService;

    public MsgEMail(DataBaseContext context, ISettingsEncryptionService? encryptionService = null)
    {
        this.context = context;
        this._encryptionService = encryptionService;
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
            var setting = context.Settings?.ToList();

            if (setting == null || !setting.Any())
            {
                return null;
            }

            var email = new EmailWrapper();

            var tmpEMailAddress = setting.FirstOrDefault(x => x.Type == Settings.APP_EMAIL_ADDRESS)?.Value;
            if (!string.IsNullOrEmpty(tmpEMailAddress))
            {
                email.EMailAddress = tmpEMailAddress;
            }

            var tmpSubject = setting.FirstOrDefault(x => x.Type == Settings.APP_SUBJECT)?.Value;
            if (!string.IsNullOrEmpty(tmpSubject))
            {
                email.Subject = tmpSubject;
            }

            email.Mark = setting.FirstOrDefault(x => x.Type == Settings.APP_MARK)?.Value ?? string.Empty;
            email.ReplyTo = setting.FirstOrDefault(x => x.Type == Settings.APP_REPLY_TO)?.Value ?? string.Empty;
            email.Outgoingserver = setting.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER)?.Value ?? string.Empty;
            email.OutgoingserverPort = setting.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_PORT)?.Value ?? string.Empty;
            email.OutgoingserverUsername = setting.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_USERNAME)?.Value ?? string.Empty;

            var encryptedPassword = setting.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_PASSWORD)?.Value ?? string.Empty;
            email.OutgoingserverPassword = DecryptPassword(encryptedPassword);

            var enableSslValue = setting.FirstOrDefault(x => x.Type == Settings.APP_ENABLE_SSL)?.Value;
            email.EnabledSsl = !string.IsNullOrEmpty(enableSslValue) && bool.TryParse(enableSslValue, out var enableSsl) && enableSsl;

            email.AuthenticationType = setting.FirstOrDefault(x => x.Type == Settings.APP_AUTHENTICATION_TYPE)?.Value ?? string.Empty;

            var readReceiptValue = setting.FirstOrDefault(x => x.Type == Settings.APP_READ_RECEIPT)?.Value;
            email.ReadReceipt = !string.IsNullOrEmpty(readReceiptValue) && bool.TryParse(readReceiptValue, out var readReceipt) && readReceipt;

            var dispositionValue = setting.FirstOrDefault(x => x.Type == Settings.APP_DISPOSITION_NOTIFICATION)?.Value;
            email.DispositionNotification = !string.IsNullOrEmpty(dispositionValue) && bool.TryParse(dispositionValue, out var disposition) && disposition;

            var timeoutValue = setting.FirstOrDefault(x => x.Type == Settings.APP_OUTGOING_SERVER_TIMEOUT)?.Value;
            email.OutgoingserverTimeout = !string.IsNullOrEmpty(timeoutValue) && int.TryParse(timeoutValue, out var timeout) ? timeout : 30000;

            return email;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email configuration error: {ex.Message}");
            return null;
        }
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
