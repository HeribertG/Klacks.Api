// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Interfaces.Email;
using MailKit.Net.Imap;
using MailKit.Security;
using MailKit;

namespace Klacks.Api.Infrastructure.Email;

public class ImapTestService : IImapTestService
{
    private readonly ILogger<ImapTestService> _logger;

    public ImapTestService(ILogger<ImapTestService> logger)
    {
        _logger = logger;
    }

    public async Task<ImapTestResult> TestConnectionAsync(ImapTestRequest request)
    {
        try
        {
            _logger.LogInformation("Testing IMAP configuration for server: {Server}:{Port} with username: {Username}",
                request.Server, request.Port, request.Username);

            if (string.IsNullOrWhiteSpace(request.Server) ||
                string.IsNullOrWhiteSpace(request.Port) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new ImapTestResult
                {
                    Success = false,
                    Message = "Missing required fields. Please fill in all required fields."
                };
            }

            if (!int.TryParse(request.Port, out var port))
            {
                return new ImapTestResult
                {
                    Success = false,
                    Message = "Invalid port number."
                };
            }

            var secureSocketOptions = GetSecureSocketOptions(port, request.EnableSSL);

            using var client = new ImapClient();
            client.Timeout = 30000;

            _logger.LogInformation("Connecting to IMAP server {Server}:{Port} with {Options}...",
                request.Server, port, secureSocketOptions);

            await client.ConnectAsync(request.Server, port, secureSocketOptions);

            _logger.LogInformation("Connected successfully. Authenticating as {Username}...", request.Username);

            await client.AuthenticateAsync(request.Username, request.Password);

            _logger.LogInformation("Authentication successful. Opening folder {Folder}...", request.Folder);

            var folder = await client.GetFolderAsync(request.Folder);
            await folder.OpenAsync(FolderAccess.ReadOnly);

            var messageCount = folder.Count;

            _logger.LogInformation("IMAP test successful. Folder {Folder} contains {Count} messages",
                request.Folder, messageCount);

            await client.DisconnectAsync(true);

            return new ImapTestResult
            {
                Success = true,
                Message = $"Connection successful! Folder '{request.Folder}' contains {messageCount} messages.",
                MessageCount = messageCount
            };
        }
        catch (AuthenticationException authEx)
        {
            _logger.LogWarning("IMAP Authentication failed: {Message}", authEx.Message);
            return new ImapTestResult
            {
                Success = false,
                Message = "Authentication failed. Please check your username and password.",
                ErrorDetails = authEx.Message
            };
        }
        catch (SslHandshakeException sslEx)
        {
            _logger.LogWarning("SSL/TLS handshake failed: {Message}", sslEx.Message);
            return new ImapTestResult
            {
                Success = false,
                Message = "SSL/TLS handshake failed. Please check your SSL settings and try a different port (993 or 143).",
                ErrorDetails = sslEx.Message
            };
        }
        catch (ImapProtocolException protocolEx)
        {
            _logger.LogWarning("IMAP protocol error: {Message}", protocolEx.Message);
            return new ImapTestResult
            {
                Success = false,
                Message = "IMAP protocol error occurred. Please check your server settings.",
                ErrorDetails = protocolEx.Message
            };
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            _logger.LogWarning("Network error: {Message}", socketEx.Message);
            return new ImapTestResult
            {
                Success = false,
                Message = "Network connection failed. Please check server address and port.",
                ErrorDetails = socketEx.Message
            };
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogWarning("Connection timeout: {Message}", timeoutEx.Message);
            return new ImapTestResult
            {
                Success = false,
                Message = "Connection timeout. Please check server address, port, and network connectivity.",
                ErrorDetails = timeoutEx.Message
            };
        }
        catch (FolderNotFoundException folderEx)
        {
            _logger.LogWarning("Folder not found: {Message}", folderEx.Message);
            return new ImapTestResult
            {
                Success = false,
                Message = $"Folder '{request.Folder}' not found on the server.",
                ErrorDetails = folderEx.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during IMAP configuration test");
            return new ImapTestResult
            {
                Success = false,
                Message = "An unexpected error occurred: " + ex.GetType().Name,
                ErrorDetails = ex.Message
            };
        }
    }

    private static SecureSocketOptions GetSecureSocketOptions(int port, bool enableSsl)
    {
        if (port == 993)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        if (port == 143)
        {
            return enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        }

        return enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
    }
}
