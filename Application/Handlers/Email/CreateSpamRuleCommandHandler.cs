// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class CreateSpamRuleCommandHandler : BaseHandler, IRequestHandler<CreateSpamRuleCommand, SpamRuleResource>
{
    private readonly ISpamRuleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpamRuleCommandHandler(
        ISpamRuleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateSpamRuleCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
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

            var mapper = new SpamRuleMapper();
            return mapper.ToResource(rule);
        }, nameof(CreateSpamRuleCommand), new { request.RuleType, request.Pattern });
    }
}
