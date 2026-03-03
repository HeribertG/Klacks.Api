// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class UpdateSpamRuleCommandHandler : BaseHandler, IRequestHandler<UpdateSpamRuleCommand, SpamRuleResource>
{
    private readonly ISpamRuleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSpamRuleCommandHandler(
        ISpamRuleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSpamRuleCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
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

            var mapper = new SpamRuleMapper();
            return mapper.ToResource(rule);
        }, nameof(UpdateSpamRuleCommand), new { request.Id });
    }
}
