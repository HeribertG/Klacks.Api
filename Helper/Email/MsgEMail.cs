using Klacks.Api.Datas;

namespace Klacks.Api.Helper.Email;

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
        var setting = context.Settings.ToList();

        var email = new EmailWrapper();
        try
        {
            var tmpEMailAddress = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_EMAIL_ADDRESS)?.Value!;
            if (tmpEMailAddress != null)
            {
                email.EMailAddress = tmpEMailAddress;
            }

            var tmpSubject = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_SUBJECT)?.Value!;
            if (tmpSubject != null)
            {
                email.Subject = tmpSubject;
            }

            email.Mark = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_MARK)?.Value!;
            email.ReplyTo = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_REPLY_TO)?.Value!;
            email.Outgoingserver = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_OUTGOING_SERVER)?.Value!;
            email.OutgoingserverPort = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_OUTGOING_SERVER_PORT)?.Value!;
            email.OutgoingserverUsername = setting.FirstOrDefault(x => x.Type == "outgoingserverUsername")?.Value!;
            email.OutgoingserverPassword = setting.FirstOrDefault(x => x.Type == "outgoingserverPassword")?.Value!;
            email.EnabledSsl = bool.Parse(setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_ENABLE_SSL)?.Value!);
            email.AuthenticationType = setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_AUTHENTICATION_TYPE)?.Value!;
            email.ReadReceipt = bool.Parse(setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_READ_RECEIPT)?.Value!);
            email.DispositionNotification = bool.Parse(setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_DISPOSITION_NOTIFICATION)?.Value!);
            email.OutgoingserverTimeout = int.Parse(setting.FirstOrDefault(x => x.Type == Constants.Settings.APP_OUTGOING_SERVER_TIMEOUT)?.Value!);

            return email;
        }
        catch
        {
            // ignored
        }

        return null;
    }
}
