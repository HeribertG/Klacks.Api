// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for testing SMTP email configurations by connecting, authenticating and sending a test email.
/// </summary>
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Klacks.Api.Infrastructure.Email;

public class EmailTestService : IEmailTestService
{
    private readonly ILogger<EmailTestService> _logger;

    public EmailTestService(ILogger<EmailTestService> logger)
    {
        this._logger = logger;
    }

    public async Task<EmailTestResult> TestConnectionAsync(EmailTestRequest request)
    {
        int port = 0;

        try
        {
            _logger.LogInformation("Testing email configuration for server: {Server}:{Port} with username: {Username}",
                request.Server, request.Port, request.Username);

            var pwd = request.Password ?? "";
            _logger.LogInformation("Password analysis: Length={Length}, StartsWithENC={StartsWithEnc}",
                pwd.Length, pwd.StartsWith("ENC:"));

            if (string.IsNullOrWhiteSpace(request.Server) ||
                string.IsNullOrWhiteSpace(request.Port) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new EmailTestResult
                {
                    Success = false,
                    Message = "Missing required fields. Please fill in all required fields.",
                    MessageKey = "EMAIL_TEST_MISSING_FIELDS"
                };
            }

            if (!int.TryParse(request.Port, out port))
            {
                return new EmailTestResult
                {
                    Success = false,
                    Message = "Invalid port number.",
                    MessageKey = "EMAIL_TEST_INVALID_PORT"
                };
            }

            if (request.AuthenticationType == "<None>" || string.IsNullOrWhiteSpace(request.AuthenticationType))
            {
                if (request.Server.Contains("outlook.com") || request.Server.Contains("hotmail.com") ||
                    request.Server.Contains("gmail.com") || request.Server.Contains("yahoo.com") ||
                    request.Server.Contains("gmx."))
                {
                    _logger.LogWarning("Authentication set to '<None>' but server {Server} typically requires authentication", request.Server);
                    return new EmailTestResult
                    {
                        Success = false,
                        Message = "Authentication required for this email provider. Please set Authentication Type to 'LOGIN' and provide valid credentials.",
                        MessageKey = "EMAIL_TEST_AUTH_REQUIRED",
                        ErrorDetails = "Most email providers require authentication"
                    };
                }
            }

            var secureSocketOptions = SecureSocketOptions.Auto;
            if (port == 465)
            {
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (port == 587 || request.EnableSSL)
            {
                secureSocketOptions = SecureSocketOptions.StartTls;
            }

            _logger.LogInformation("SSL Configuration: Port={Port}, RequestEnableSSL={RequestSSL}, SecureSocketOptions={Options}",
                port, request.EnableSSL, secureSocketOptions);

            using var client = new SmtpClient();

            var timeout = Math.Max(request.Timeout, 30000);
            client.Timeout = timeout;

            _logger.LogInformation("Connecting to SMTP server {Server}:{Port} with {Options}...",
                request.Server, port, secureSocketOptions);

            await client.ConnectAsync(request.Server, port, secureSocketOptions);

            _logger.LogInformation("Connected successfully. Authenticating as {Username}...", request.Username);

            await client.AuthenticateAsync(request.Username, request.Password);

            _logger.LogInformation("Authentication successful. Sending test email...");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(request.Username, request.Username));
            message.To.Add(new MailboxAddress(request.Username, request.Username));
            message.Subject = "Klacks Email Configuration Test";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<html>
<body>
<h2>Email Configuration Test Successful!</h2>
<p>This is a test email from your Klacks application to verify that your email configuration is working correctly.</p>
<p><strong>Test Details:</strong></p>
<ul>
<li>SMTP Server: {request.Server}</li>
<li>Port: {request.Port}</li>
<li>SSL/TLS: {secureSocketOptions}</li>
<li>Authentication: {request.AuthenticationType}</li>
<li>Test Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</li>
</ul>
<p>If you receive this email, your email configuration is working properly.</p>
<p><em>This is an automated test message from Klacks.</em></p>
</body>
</html>"
            };
            message.Body = bodyBuilder.ToMessageBody();

            await client.SendAsync(message);

            _logger.LogInformation("Test email sent successfully to {Username}", request.Username);

            await client.DisconnectAsync(true);

            return new EmailTestResult
            {
                Success = true,
                Message = $"Test email sent successfully to {request.Username}! Please check your inbox to confirm delivery.",
                MessageKey = "EMAIL_TEST_SUCCESS_SENT",
                MessageParams = new Dictionary<string, string> { { "email", request.Username } }
            };
        }
        catch (AuthenticationException authEx)
        {
            _logger.LogWarning("SMTP Authentication failed: {Message}", authEx.Message);

            string authMessage = "Authentication failed. Please check your username and password.";
            string messageKey = "EMAIL_TEST_AUTH_FAILED";

            if (request.Server.Contains("gmx."))
            {
                authMessage = "GMX Authentication failed. Please verify:\n" +
                            "1. Use your full email address as username\n" +
                            "2. Use your GMX password\n" +
                            "3. Ensure SMTP/IMAP is enabled in your GMX account settings";
                messageKey = "EMAIL_TEST_AUTH_FAILED_GMX";
            }
            else if (request.Server.Contains("outlook.com") || request.Server.Contains("hotmail.com"))
            {
                authMessage = "Authentication failed. For Microsoft accounts, use an App Password instead of your regular password.";
                messageKey = "EMAIL_TEST_AUTH_FAILED_MICROSOFT";
            }

            return new EmailTestResult
            {
                Success = false,
                Message = authMessage,
                MessageKey = messageKey,
                ErrorDetails = authEx.Message
            };
        }
        catch (SslHandshakeException sslEx)
        {
            _logger.LogWarning("SSL/TLS handshake failed: {Message}", sslEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = "SSL/TLS handshake failed. Please check your SSL settings and try a different port (587 or 465).",
                MessageKey = "EMAIL_TEST_SSL_FAILED",
                ErrorDetails = sslEx.Message
            };
        }
        catch (SmtpCommandException smtpEx)
        {
            _logger.LogWarning("SMTP command error: {StatusCode} - {Message}", smtpEx.StatusCode, smtpEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = $"SMTP error: {smtpEx.Message}",
                MessageKey = "EMAIL_TEST_SMTP_COMMAND_ERROR",
                ErrorDetails = $"Status: {smtpEx.StatusCode}"
            };
        }
        catch (SmtpProtocolException protocolEx)
        {
            _logger.LogWarning("SMTP protocol error: {Message}", protocolEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = "SMTP protocol error occurred. Please check your server settings.",
                MessageKey = "EMAIL_TEST_PROTOCOL_ERROR",
                ErrorDetails = protocolEx.Message
            };
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            _logger.LogWarning("Network error: {Message}", socketEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = "Network connection failed. Please check server address and port.",
                MessageKey = "EMAIL_TEST_NETWORK_ERROR",
                ErrorDetails = socketEx.Message
            };
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogWarning("Connection timeout: {Message}", timeoutEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = "Connection timeout. Please check server address, port, and network connectivity.",
                MessageKey = "EMAIL_TEST_TIMEOUT",
                ErrorDetails = timeoutEx.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email configuration test");
            return new EmailTestResult
            {
                Success = false,
                Message = "An unexpected error occurred: " + ex.GetType().Name,
                MessageKey = "EMAIL_TEST_UNEXPECTED",
                ErrorDetails = ex.Message
            };
        }
    }
}
