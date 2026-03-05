// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class EmailFolderSeedService
{
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IReceivedEmailRepository _emailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailFolderSeedService> _logger;

    public EmailFolderSeedService(
        IEmailFolderRepository folderRepository,
        IReceivedEmailRepository emailRepository,
        IUnitOfWork unitOfWork,
        ILogger<EmailFolderSeedService> logger)
    {
        _folderRepository = folderRepository;
        _emailRepository = emailRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var changed = false;

        changed |= await MigrateOldFolderAsync("Junk");
        changed |= await MigrateOldFolderAsync("$trash");

        if (changed)
        {
            await _unitOfWork.CompleteAsync();
        }
    }

    private async Task<bool> MigrateOldFolderAsync(string oldImapName)
    {
        var oldFolder = await _folderRepository.GetByImapNameAsync(oldImapName);
        if (oldFolder == null) return false;

        await _folderRepository.DeleteAsync(oldFolder.Id);
        var movedCount = await _emailRepository.BulkMoveFolderAsync(oldImapName, "INBOX");
        _logger.LogInformation("Removed legacy folder {OldName}, moved {Count} emails to INBOX.", oldImapName, movedCount);

        return true;
    }
}
