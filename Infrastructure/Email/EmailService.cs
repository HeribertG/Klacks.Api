// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly DataBaseContext _context;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        DataBaseContext context,
        ISettingsEncryptionService encryptionService,
        ILogger<EmailService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public string SendMail(string email, string title, string message)
    {
        var mail = new MsgEMail(_context, _logger, _encryptionService);
        return mail.SendMail(email, title, message);
    }

    public async Task<bool> CanSendEmailAsync()
    {
        try
        {
            if (_context?.Settings == null)
                return false;

            await _context.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
