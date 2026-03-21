// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Infrastructure.Email;

public class EmailReclassificationTrigger : IEmailReclassificationTrigger
{
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailReclassificationTrigger> _logger;

    public EmailReclassificationTrigger(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailReclassificationTrigger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void TriggerReclassification()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await ReclassifyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background email reclassification failed");
            }
        });
    }

    private async Task ReclassifyAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IReceivedEmailRepository>();
        var folderRepository = scope.ServiceProvider.GetRequiredService<IEmailFolderRepository>();
        var spamFilterService = scope.ServiceProvider.GetRequiredService<ISpamFilterService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IImapEmailService>();
        var assignmentService = scope.ServiceProvider.GetRequiredService<IEmailClientAssignmentService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

        var inboxFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
        if (string.IsNullOrEmpty(inboxFolder)) return;

        var junkFolder = await folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);
        if (string.IsNullOrEmpty(junkFolder)) return;

        var movedToJunk = 0;
        var movedFromJunk = 0;

        movedToJunk += await ReclassifyFolderAsync(emailRepository, spamFilterService, emailService,
            inboxFolder, junkFolder, isSpamCheck: true);

        movedToJunk += await ReclassifyFolderAsync(emailRepository, spamFilterService, emailService,
            EmailConstants.ClientAssignedFolder, junkFolder, isSpamCheck: true);

        movedFromJunk += await ReclassifyFolderAsync(emailRepository, spamFilterService, emailService,
            junkFolder, inboxFolder, isSpamCheck: false);

        if (movedToJunk > 0 || movedFromJunk > 0)
        {
            await unitOfWork.CompleteAsync();

            if (movedFromJunk > 0)
            {
                await assignmentService.AssignInboxEmailsToClientsAsync();
            }

            await notificationService.NotifyNewEmailsAsync(movedToJunk + movedFromJunk);
            _logger.LogInformation(
                "Background reclassification: {ToJunk} to junk, {FromJunk} restored from junk",
                movedToJunk, movedFromJunk);
        }
    }

    private async Task<int> ReclassifyFolderAsync(
        IReceivedEmailRepository emailRepository,
        ISpamFilterService spamFilterService,
        IImapEmailService emailService,
        string sourceFolder,
        string targetFolder,
        bool isSpamCheck)
    {
        var movedCount = 0;
        var skip = 0;

        while (true)
        {
            var emails = await emailRepository.GetListByFolderAsync(sourceFolder, skip, BatchSize);
            if (emails.Count == 0) break;

            foreach (var email in emails)
            {
                var result = await spamFilterService.ClassifyAsync(email, CancellationToken.None);
                var shouldMove = isSpamCheck ? result.IsSpam : !result.IsSpam;

                if (shouldMove)
                {
                    var imapSource = isSpamCheck
                        ? (string.IsNullOrEmpty(email.SourceImapFolder) ? sourceFolder : email.SourceImapFolder)
                        : sourceFolder;
                    var imapTarget = isSpamCheck ? targetFolder
                        : (string.IsNullOrEmpty(email.SourceImapFolder) ? targetFolder : email.SourceImapFolder);

                    await emailRepository.MoveToFolderAsync(email.Id, isSpamCheck ? targetFolder : imapTarget);
                    await emailService.MoveEmailOnImapAsync(email.ImapUid, imapSource, imapTarget, CancellationToken.None);
                    movedCount++;
                }
            }

            if (emails.Count < BatchSize) break;
            skip += BatchSize;
        }

        return movedCount;
    }
}
