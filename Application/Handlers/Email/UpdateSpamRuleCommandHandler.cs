// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class UpdateSpamRuleCommandHandler : BaseHandler, IRequestHandler<UpdateSpamRuleCommand, SpamRuleResource>
{
    private readonly ISpamRuleRepository _repository;
    private readonly IReceivedEmailRepository _emailRepository;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly ISpamFilterService _spamFilterService;
    private readonly IImapEmailService _imapEmailService;
    private readonly IEmailClientAssignmentService _assignmentService;
    private readonly IEmailNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSpamRuleCommandHandler(
        ISpamRuleRepository repository,
        IReceivedEmailRepository emailRepository,
        IEmailFolderRepository folderRepository,
        ISpamFilterService spamFilterService,
        IImapEmailService imapEmailService,
        IEmailClientAssignmentService assignmentService,
        IEmailNotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSpamRuleCommandHandler> logger)
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

    public async Task<SpamRuleResource> Handle(UpdateSpamRuleCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rule = await _repository.GetByIdAsync(request.Id);
            if (rule == null)
            {
                throw new KeyNotFoundException($"Spam rule with id {request.Id} not found.");
            }

            rule.RuleType = request.RuleType;
            rule.Pattern = request.Pattern;
            rule.IsActive = request.IsActive;
            rule.SortOrder = request.SortOrder;

            await _repository.UpdateAsync(rule);
            await _unitOfWork.CompleteAsync();

            await ReclassifyAllEmailsAsync(cancellationToken);

            var mapper = new SpamRuleMapper();
            return mapper.ToResource(rule);
        }, nameof(UpdateSpamRuleCommand), new { request.Id });
    }

    private async Task ReclassifyAllEmailsAsync(CancellationToken cancellationToken)
    {
        var inboxFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
        if (string.IsNullOrEmpty(inboxFolder)) return;

        var junkFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);
        if (string.IsNullOrEmpty(junkFolder)) return;

        var movedToJunk = 0;
        var movedFromJunk = 0;

        var inboxEmails = await _emailRepository.GetListByFolderAsync(inboxFolder, 0, int.MaxValue);
        foreach (var email in inboxEmails)
        {
            var result = await _spamFilterService.ClassifyAsync(email, cancellationToken);
            if (result.IsSpam)
            {
                await _emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                await _imapEmailService.MoveEmailOnImapAsync(email.ImapUid, inboxFolder, junkFolder, cancellationToken);
                movedToJunk++;
            }
        }

        var clientAssignedEmails = await _emailRepository.GetListByFolderAsync(EmailConstants.ClientAssignedFolder, 0, int.MaxValue);
        foreach (var email in clientAssignedEmails)
        {
            var result = await _spamFilterService.ClassifyAsync(email, cancellationToken);
            if (result.IsSpam)
            {
                await _emailRepository.MoveToFolderAsync(email.Id, junkFolder);
                await _imapEmailService.MoveEmailOnImapAsync(email.ImapUid, email.SourceImapFolder, junkFolder, cancellationToken);
                movedToJunk++;
            }
        }

        var junkEmails = await _emailRepository.GetListByFolderAsync(junkFolder, 0, int.MaxValue);
        foreach (var email in junkEmails)
        {
            var result = await _spamFilterService.ClassifyAsync(email, cancellationToken);
            if (!result.IsSpam)
            {
                var targetFolder = string.IsNullOrEmpty(email.SourceImapFolder) ? inboxFolder : email.SourceImapFolder;
                await _emailRepository.MoveToFolderAsync(email.Id, targetFolder);
                await _imapEmailService.MoveEmailOnImapAsync(email.ImapUid, junkFolder, targetFolder, cancellationToken);
                movedFromJunk++;
            }
        }

        if (movedToJunk > 0 || movedFromJunk > 0)
        {
            await _unitOfWork.CompleteAsync();

            if (movedFromJunk > 0)
            {
                await _assignmentService.AssignInboxEmailsToClientsAsync();
            }

            await _notificationService.NotifyNewEmailsAsync(movedToJunk + movedFromJunk);
            _logger.LogInformation(
                "Reclassified after rule update: {ToJunk} to junk, {FromJunk} restored from junk",
                movedToJunk, movedFromJunk);
        }
    }
}
