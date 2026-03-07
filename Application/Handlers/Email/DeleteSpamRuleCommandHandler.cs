// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class DeleteSpamRuleCommandHandler : BaseHandler, IRequestHandler<DeleteSpamRuleCommand, bool>
{
    private readonly ISpamRuleRepository _repository;
    private readonly IReceivedEmailRepository _emailRepository;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly ISpamFilterService _spamFilterService;
    private readonly IImapEmailService _imapEmailService;
    private readonly IEmailClientAssignmentService _assignmentService;
    private readonly IEmailNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSpamRuleCommandHandler(
        ISpamRuleRepository repository,
        IReceivedEmailRepository emailRepository,
        IEmailFolderRepository folderRepository,
        ISpamFilterService spamFilterService,
        IImapEmailService imapEmailService,
        IEmailClientAssignmentService assignmentService,
        IEmailNotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteSpamRuleCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailRepository = emailRepository;
        _folderRepository = folderRepository;
        _spamFilterService = spamFilterService;
        _imapEmailService = imapEmailService;
        _assignmentService = assignmentService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteSpamRuleCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.DeleteAsync(request.Id);
            await _unitOfWork.CompleteAsync();

            await ReclassifyJunkEmailsAsync(cancellationToken);

            return true;
        }, nameof(DeleteSpamRuleCommand), new { request.Id });
    }

    private async Task ReclassifyJunkEmailsAsync(CancellationToken cancellationToken)
    {
        var inboxFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
        if (string.IsNullOrEmpty(inboxFolder)) return;

        var junkFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);
        if (string.IsNullOrEmpty(junkFolder)) return;

        var junkEmails = await _emailRepository.GetListByFolderAsync(junkFolder, 0, int.MaxValue);
        var restoredCount = 0;

        foreach (var email in junkEmails)
        {
            var result = await _spamFilterService.ClassifyAsync(email, cancellationToken);
            if (!result.IsSpam)
            {
                var targetFolder = string.IsNullOrEmpty(email.SourceImapFolder) ? inboxFolder : email.SourceImapFolder;
                await _emailRepository.MoveToFolderAsync(email.Id, targetFolder);
                await _imapEmailService.MoveEmailOnImapAsync(email.ImapUid, junkFolder, targetFolder, cancellationToken);
                restoredCount++;
            }
        }

        if (restoredCount > 0)
        {
            await _unitOfWork.CompleteAsync();
            await _assignmentService.AssignInboxEmailsToClientsAsync();
            await _notificationService.NotifyNewEmailsAsync(restoredCount);
            _logger.LogInformation("Restored {Count} emails from junk after rule deletion", restoredCount);
        }
    }
}
