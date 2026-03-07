// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Infrastructure.Email;

public class EmailReclassificationTrigger : IEmailReclassificationTrigger
{
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

        var inboxEmails = await emailRepository.GetListByFolderAsync(inboxFolder, 0, int.MaxValue);
        foreach (var email in inboxEmails)
        {
            var result = await spamFilterService.ClassifyAsync(email, CancellationToken.None);
            if (result.IsSpam)
            {
                await emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                await emailService.MoveEmailOnImapAsync(email.ImapUid, inboxFolder, junkFolder, CancellationToken.None);
                movedToJunk++;
            }
        }

        var clientAssignedEmails = await emailRepository.GetListByFolderAsync(EmailConstants.ClientAssignedFolder, 0, int.MaxValue);
        foreach (var email in clientAssignedEmails)
        {
            var result = await spamFilterService.ClassifyAsync(email, CancellationToken.None);
            if (result.IsSpam)
            {
                await emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                await emailService.MoveEmailOnImapAsync(email.ImapUid, email.SourceImapFolder, junkFolder, CancellationToken.None);
                movedToJunk++;
            }
        }

        var junkEmails = await emailRepository.GetListByFolderAsync(junkFolder, 0, int.MaxValue);
        foreach (var email in junkEmails)
        {
            var result = await spamFilterService.ClassifyAsync(email, CancellationToken.None);
            if (!result.IsSpam)
            {
                var targetFolder = string.IsNullOrEmpty(email.SourceImapFolder) ? inboxFolder : email.SourceImapFolder;
                await emailRepository.MoveToFolderAsync(email.Id, targetFolder);
                await emailService.MoveEmailOnImapAsync(email.ImapUid, junkFolder, targetFolder, CancellationToken.None);
                movedFromJunk++;
            }
        }

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
}
