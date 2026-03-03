// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class EmailFolderSeedService
{
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailFolderSeedService> _logger;

    public EmailFolderSeedService(
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork,
        ILogger<EmailFolderSeedService> logger)
    {
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var seeded = false;

        if (!await _folderRepository.ExistsByImapNameAsync(EmailConstants.InboxFolder))
        {
            await _folderRepository.AddAsync(new EmailFolder
            {
                Name = "Inbox",
                ImapFolderName = EmailConstants.InboxFolder,
                IsSystem = true,
                SortOrder = 0
            });
            seeded = true;
            _logger.LogInformation("Seeded inbox email folder.");
        }

        if (!await _folderRepository.ExistsByImapNameAsync(EmailConstants.JunkFolder))
        {
            await _folderRepository.AddAsync(new EmailFolder
            {
                Name = "Junk",
                ImapFolderName = EmailConstants.JunkFolder,
                IsSystem = true,
                SortOrder = 900
            });
            seeded = true;
            _logger.LogInformation("Seeded junk email folder.");
        }

        if (!await _folderRepository.ExistsByImapNameAsync(EmailConstants.TrashFolder))
        {
            await _folderRepository.AddAsync(new EmailFolder
            {
                Name = "Deleted Items",
                ImapFolderName = EmailConstants.TrashFolder,
                IsSystem = true,
                SortOrder = 999
            });
            seeded = true;
            _logger.LogInformation("Seeded trash email folder.");
        }

        if (seeded)
        {
            await _unitOfWork.CompleteAsync();
        }
    }
}
