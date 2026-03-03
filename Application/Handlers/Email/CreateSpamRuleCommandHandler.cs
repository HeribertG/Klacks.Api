// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class CreateSpamRuleCommandHandler : BaseHandler, IRequestHandler<CreateSpamRuleCommand, SpamRuleResource>
{
    private readonly ISpamRuleRepository _repository;
    private readonly IReceivedEmailRepository _emailRepository;
    private readonly ISpamFilterService _spamFilterService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpamRuleCommandHandler(
        ISpamRuleRepository repository,
        IReceivedEmailRepository emailRepository,
        ISpamFilterService spamFilterService,
        IUnitOfWork unitOfWork,
        ILogger<CreateSpamRuleCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailRepository = emailRepository;
        _spamFilterService = spamFilterService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SpamRuleResource> Handle(CreateSpamRuleCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var allRules = await _repository.GetAllAsync();
            var maxSortOrder = allRules.Count > 0 ? allRules.Max(r => r.SortOrder) : 0;

            var rule = new SpamRule
            {
                RuleType = request.RuleType,
                Pattern = request.Pattern,
                IsActive = true,
                SortOrder = maxSortOrder + 1
            };

            await _repository.AddAsync(rule);
            await _unitOfWork.CompleteAsync();

            await ReclassifyInboxEmailsAsync(cancellationToken);

            var mapper = new SpamRuleMapper();
            return mapper.ToResource(rule);
        }, nameof(CreateSpamRuleCommand), new { request.RuleType, request.Pattern });
    }

    private async Task ReclassifyInboxEmailsAsync(CancellationToken cancellationToken)
    {
        var inboxEmails = await _emailRepository.GetListByFolderAsync(EmailConstants.InboxFolder, 0, int.MaxValue);
        var movedCount = 0;

        foreach (var email in inboxEmails)
        {
            var result = await _spamFilterService.ClassifyAsync(email, cancellationToken);
            if (result.IsSpam)
            {
                await _emailRepository.MoveToFolderAsync(email.Id, EmailConstants.JunkFolder);
                movedCount++;
            }
        }

        if (movedCount > 0)
        {
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Reclassified {Count} emails as spam after new rule", movedCount);
        }
    }
}
