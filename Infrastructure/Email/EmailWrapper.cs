// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Net.Mail;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Email;

public class EmailWrapper
{
    internal const string NoSenderAddressMessage = "No sender address available";

    internal const string LineBreakInHeaderMessage = "Email header values must not contain line breaks";

    private static readonly char[] LineBreakCharacters = ['\r', '\n'];

    public string AuthenticationType { get; set; } = string.Empty;

    public bool DispositionNotification { get; set; } = false;

    public string EMailAddress { get; set; } = string.Empty;

    public string EMailMessage { get; set; } = string.Empty;

    public bool EnabledSsl { get; set; } = false;

    public string Mark { get; set; } = string.Empty;

    public string Outgoingserver { get; set; } = string.Empty;

    public string OutgoingserverPassword { get; set; } = string.Empty;

    public string OutgoingserverPort { get; set; } = string.Empty;

    public int OutgoingserverTimeout { get; set; } = 1000;

    public string OutgoingserverUsername { get; set; } = string.Empty;

    public bool ReadReceipt { get; set; } = false;

    public string ReplyTo { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string SendEmailMessage(string strTo, string strSubject, string strMessage, string[] fileList)
    {
        string address = EMailAddress;
        MailMessage mailMsg = SetAddress(strTo, address);

        SetBody(strSubject, strMessage, mailMsg);

        long lenght = 0;
        var count = 0;

        // attach each file attachment
        foreach (var strfile in fileList)
        {
            count++;
            if (!string.IsNullOrEmpty(strfile))
            {
                try
                {
                    var msgAttach = new Attachment(strfile);
                    lenght += msgAttach.ContentStream.Length;

                    mailMsg.Attachments.Add(msgAttach);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        return SendEmail(mailMsg);
    }

    public string SendEmailMessage(string strTo, string strSubject, string strMessage, string file)
    {
        if (string.IsNullOrEmpty(ReplyTo))
        {
            return NoSenderAddressMessage;
        }

        MailMessage mailMsg = SetAddress(strTo, ReplyTo);
        SetBody(strSubject, strMessage, mailMsg);

        if (!string.IsNullOrEmpty(file))
        {
            var msgAttach = new Attachment(file);
            mailMsg.Attachments.Add(msgAttach);
        }

        return SendEmail(mailMsg);
    }

    public string SendReplyMessage(string strTo, string strSubject, string strMessage, IReadOnlyDictionary<string, string> headers)
    {
        using var mailMsg = BuildReplyMessage(strTo, strSubject, strMessage, headers);
        return mailMsg == null ? NoSenderAddressMessage : SendEmail(mailMsg);
    }

    internal MailMessage? BuildReplyMessage(string strTo, string strSubject, string strMessage, IReadOnlyDictionary<string, string> headers)
    {
        if (string.IsNullOrEmpty(ReplyTo))
        {
            return null;
        }

        var subject = (strSubject ?? string.Empty).Trim();
        EnsureSingleLine(subject);
        EnsureSingleLine(strTo);
        foreach (var (name, value) in headers)
        {
            EnsureSingleLine(name);
            EnsureSingleLine(value);
        }

        var recipient = new MailAddress(strTo.Trim());
        var mailMsg = CreateMessageFrom(ReplyTo);
        mailMsg.To.Add(recipient);
        mailMsg.Subject = subject;
        mailMsg.Body = (strMessage ?? string.Empty).Trim() + Environment.NewLine;
        mailMsg.IsBodyHtml = false;

        foreach (var (name, value) in headers)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                mailMsg.Headers.Add(name, value);
            }
        }

        return mailMsg;
    }

    private static void EnsureSingleLine(string? value)
    {
        if (value != null && value.IndexOfAny(LineBreakCharacters) >= 0)
        {
            throw new ArgumentException(LineBreakInHeaderMessage);
        }
    }

    private string SendEmail(MailMessage mailMsg)
    {
        using var smtpMail = !string.IsNullOrEmpty(OutgoingserverPort)
            ? new SmtpClient(Outgoingserver, int.Parse(OutgoingserverPort))
            : new SmtpClient(Outgoingserver);

        switch (AuthenticationType)
        {
            case "<None>":
            case "":
                {
                    smtpMail.UseDefaultCredentials = false;
                    AuthenticationType = string.Empty;
                    break;
                }

            default:
                {
                    smtpMail.UseDefaultCredentials = false;
                    break;
                }
        }

        try
        {
            var basicAuthenticationInfo = new System.Net.NetworkCredential(OutgoingserverUsername, OutgoingserverPassword);
            smtpMail.Credentials = basicAuthenticationInfo.GetCredential(smtpMail.Host, smtpMail.Port, AuthenticationType);
            smtpMail.EnableSsl = EnabledSsl;
            smtpMail.Send(mailMsg);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return EmailConstants.SendSucceededResult;
    }

    private MailMessage SetAddress(string strTo, string address)
    {
        var mailMsg = CreateMessageFrom(address);

        var tmpTo = strTo.Split(";");

        foreach (var tmp in tmpTo)
        {
            if (!string.IsNullOrEmpty(tmp))
            {
                mailMsg.To.Add(new MailAddress(tmp.Trim()));
            }
        }

        return mailMsg;
    }

    private MailMessage CreateMessageFrom(string address) =>
        string.IsNullOrEmpty(Mark)
            ? new MailMessage() { From = new MailAddress(address.Trim()) }
            : new MailMessage() { From = new MailAddress(Mark + "<" + address.Trim() + ">") };

    private void SetBody(string strSubject, string strMessage, MailMessage mailMsg)
    {
        if (string.IsNullOrEmpty(strSubject))
        {
            strSubject = string.Empty;
        }

        if (string.IsNullOrEmpty(strMessage))
        {
            strMessage = string.Empty;
        }

        mailMsg.Subject = strSubject.Trim();
        mailMsg.Body = strMessage.Trim() + Environment.NewLine;
        mailMsg.IsBodyHtml = true;
    }
}
