// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Email;

public class EmailClientAssignmentService : IEmailClientAssignmentService
{
    private readonly DataBaseContext _context;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly ILogger<EmailClientAssignmentService> _logger;

    public EmailClientAssignmentService(
        DataBaseContext context,
        IEmailFolderRepository folderRepository,
        ILogger<EmailClientAssignmentService> logger)
    {
        _context = context;
        _folderRepository = folderRepository;
        _logger = logger;
    }

    public async Task AssignInboxEmailsToClientsAsync()
    {
        var inboxFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
        if (string.IsNullOrEmpty(inboxFolder)) return;

        var assignedCount = await _context.ReceivedEmails
            .Where(e => e.Folder == inboxFolder &&
                _context.Set<Communication>()
                    .Where(c => !c.IsDeleted &&
                        (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                        c.Value != null && c.Value != "")
                    .Any(c => c.Value!.ToLower() == e.FromAddress.ToLower()))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Folder, EmailConstants.ClientAssignedFolder));

        if (assignedCount > 0)
        {
            _logger.LogInformation("Assigned {Count} inbox emails to client folders", assignedCount);
        }
    }

    public async Task AssignNewEmailAsync(ReceivedEmail email)
    {
        var inboxFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
        if (!string.Equals(email.Folder, inboxFolder, StringComparison.OrdinalIgnoreCase))
            return;

        var clientEmailAddresses = await GetClientEmailAddressesAsync();
        if (clientEmailAddresses.Contains(email.FromAddress))
        {
            email.Folder = EmailConstants.ClientAssignedFolder;
        }
    }

    private async Task<HashSet<string>> GetClientEmailAddressesAsync()
    {
        var addresses = await _context.Set<Communication>()
            .Where(c => !c.IsDeleted &&
                        (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                        !string.IsNullOrEmpty(c.Value))
            .Select(c => c.Value)
            .ToListAsync();

        return new HashSet<string>(addresses, StringComparer.OrdinalIgnoreCase);
    }
}
