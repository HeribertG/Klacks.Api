using System.Net.Mail;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Infrastructure.Email;

public interface IEmailTestService
{
    Task<EmailTestResult> TestConnectionAsync(EmailTestRequest request);
}

public class EmailTestService : IEmailTestService
{
    private readonly ILogger<EmailTestService> _logger;

    public EmailTestService(ILogger<EmailTestService> logger)
    {
        _logger = logger;
    }

    public async Task<EmailTestResult> TestConnectionAsync(EmailTestRequest request)
    {
        int port = 0;

        try
        {
            _logger.LogInformation("Testing email configuration for server: {Server}:{Port} with username: {Username}",
                request.Server, request.Port, request.Username);

            if (string.IsNullOrWhiteSpace(request.Server) ||
                string.IsNullOrWhiteSpace(request.Port) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new EmailTestResult
                {
                    Success = false,
                    Message = "Missing required fields. Please fill in all required fields."
                };
            }

            if (!int.TryParse(request.Port, out port))
            {
                return new EmailTestResult
                {
                    Success = false,
                    Message = "Invalid port number."
                };
            }

            using var client = new SmtpClient(request.Server, port);

            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Timeout = Math.Max(request.Timeout, 30000);

            if (port == 465)
            {
                client.EnableSsl = true;
                _logger.LogInformation("Using implicit SSL for port 465");
            }
            else if (port == 587)
            {
                client.EnableSsl = request.EnableSSL;
                _logger.LogInformation("Using explicit SSL (STARTTLS) for port 587: {EnableSSL}", request.EnableSSL);
            }
            else
            {
                client.EnableSsl = request.EnableSSL;
            }

            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls13;

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
                        ErrorDetails = "Most email providers require authentication"
                    };
                }

                client.UseDefaultCredentials = true;
            }
            else
            {
                client.UseDefaultCredentials = false;

                var networkCredential = new System.Net.NetworkCredential(request.Username, request.Password);
                client.Credentials = networkCredential;

                _logger.LogInformation("Configuring authentication - Username: {Username}, Server: {Server}, Port: {Port}, SSL: {SSL}, AuthType: {AuthType}",
                    request.Username, request.Server, port, request.EnableSSL, request.AuthenticationType);

                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Username or password is empty!");
                    return new EmailTestResult
                    {
                        Success = false,
                        Message = "Username and password are required for authentication.",
                        ErrorDetails = "Missing credentials"
                    };
                }
            }

            _logger.LogInformation("SMTP Client configured: Server={Server}, Port={Port}, SSL={SSL}, Timeout={Timeout}ms",
                request.Server, port, request.EnableSSL, client.Timeout);

            var testMessage = new MailMessage
            {
                From = new MailAddress(request.Username),
                Subject = "Klacks Email Configuration Test",
                Body = @"
<html>
<body>
<h2>Email Configuration Test Successful!</h2>
<p>This is a test email from your Klacks application to verify that your email configuration is working correctly.</p>
<p><strong>Test Details:</strong></p>
<ul>
<li>SMTP Server: " + request.Server + @"</li>
<li>Port: " + request.Port + @"</li>
<li>SSL Enabled: " + request.EnableSSL + @"</li>
<li>Authentication: " + request.AuthenticationType + @"</li>
<li>Test Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</li>
</ul>
<p>If you receive this email, your email configuration is working properly.</p>
<p><em>This is an automated test message from Klacks.</em></p>
</body>
</html>",
                IsBodyHtml = true
            };
            testMessage.To.Add(request.Username);

            _logger.LogInformation("Attempting authentication with UseDefaultCredentials: {UseDefault}, Credentials null: {CredsNull}",
    client.UseDefaultCredentials, client.Credentials == null);

            try
            {
                await Task.Run(() =>
                {
                    _logger.LogInformation("Attempting to send test email to {Username} via {Server}:{Port}",
                        request.Username, request.Server, port);
                    client.Send(testMessage);
                    _logger.LogInformation("Test email sent successfully to {Username}", request.Username);
                });
            }
            catch (Exception sendEx)
            {
                _logger.LogError(sendEx, "Failed to send test email");
                throw;
            }

            _logger.LogInformation("Email configuration test successful for {Server}:{Port}", request.Server, port);

            return new EmailTestResult
            {
                Success = true,
                Message = $"Test email sent successfully to {request.Username}! Please check your inbox to confirm delivery."
            };
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogWarning("SMTP error occurred: {Message}", smtpEx.Message);

            if (smtpEx.Message.Contains("timed out") || smtpEx.Message.Contains("timeout"))
            {
                return new EmailTestResult
                {
                    Success = false,
                    Message = "Connection timeout. Please check server address, port, and network connectivity. For Outlook/Hotmail use: smtp-mail.outlook.com:587 with SSL.",
                    ErrorDetails = smtpEx.Message
                };
            }


            if (smtpEx.Message.Contains("authentication") ||
                smtpEx.Message.Contains("Authentication") ||
                smtpEx.Message.Contains("5.7.8") ||
                smtpEx.Message.Contains("5.7.3") ||
                smtpEx.Message.Contains("535") ||
                smtpEx.Message.Contains("Login failed") ||
                smtpEx.Message.Contains("Invalid credentials") ||
                smtpEx.Message.Contains("Username and Password not accepted") ||
                smtpEx.Message.Contains("secure connection") ||
                smtpEx.Message.Contains("client was not authenticated"))
            {
                string authMessage = "Authentication failed. Please check your username and password.";

                if (request.Server.Contains("gmx."))
                {
                    authMessage = "GMX Authentication failed. Please verify:\n" +
                                "1. Use your full email address as username\n" +
                                "2. Use your GMX password (not an app password)\n" +
                                "3. Ensure SMTP/IMAP is enabled in your GMX account settings\n" +
                                "4. Try using port 587 with SSL enabled";
                }
                else if (request.Server.Contains("outlook.com") || request.Server.Contains("hotmail.com"))
                {
                    authMessage = "Authentication failed. For Microsoft accounts, use an App Password instead of your regular password.";
                }

                return new EmailTestResult
                {
                    Success = false,
                    Message = authMessage,
                    ErrorDetails = smtpEx.Message
                };
            }

            if (smtpEx.Message.Contains("connection") ||
                smtpEx.Message.Contains("refused") ||
                smtpEx.Message.Contains("Failure sending mail") ||
                smtpEx.Message.Contains("Unable to read data"))
            {
                string connectionMessage = "Failed to connect to SMTP server. Please check server address, port, and SSL settings.";

                if (request.Server.Contains("gmx.") && port == 465)
                {
                    connectionMessage = "GMX Connection failed with port 465. Please try:\n" +
                                      "1. Use port 587 instead of 465\n" +
                                      "2. Ensure SSL is enabled\n" +
                                      "3. Check that external applications are allowed in GMX settings";
                }

                return new EmailTestResult
                {
                    Success = false,
                    Message = connectionMessage,
                    ErrorDetails = smtpEx.Message
                };
            }

            return new EmailTestResult
            {
                Success = false,
                Message = "SMTP error occurred: " + smtpEx.Message,
                ErrorDetails = smtpEx.Message
            };
        }
        catch (System.Security.Authentication.AuthenticationException authEx)
        {
            _logger.LogWarning("Authentication error: {Message}", authEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = "SSL/TLS authentication failed. Please check your SSL settings.",
                ErrorDetails = authEx.Message
            };
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            _logger.LogWarning("Network error: {Message}", socketEx.Message);
            return new EmailTestResult
            {
                Success = false,
                Message = "Network connection failed. Please check server address and port.",
                ErrorDetails = socketEx.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email configuration test");
            return new EmailTestResult
            {
                Success = false,
                Message = "An unexpected error occurred: " + ex.GetType().Name,
                ErrorDetails = ex.Message
            };
        }
    }
}