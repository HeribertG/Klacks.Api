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
        var exists = await _folderRepository.ExistsByImapNameAsync(EmailConstants.TrashFolder);
        if (exists)
        {
            _logger.LogInformation("Trash folder already exists, skipping seed.");
            return;
        }

        var trashFolder = new EmailFolder
        {
            Name = "Deleted Items",
            ImapFolderName = EmailConstants.TrashFolder,
            IsSystem = true,
            SortOrder = 999
        };

        await _folderRepository.AddAsync(trashFolder);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Seeded trash email folder.");
    }
}
