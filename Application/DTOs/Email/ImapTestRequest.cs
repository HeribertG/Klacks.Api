// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class ImapTestRequest
{
    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 993;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSSL { get; set; }

    public string Folder { get; set; } = "INBOX";
}
