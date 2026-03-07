// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class DeleteSpamRuleCommandHandler : BaseHandler, IRequestHandler<DeleteSpamRuleCommand, bool>
{
    private readonly ISpamRuleRepository _repository;
    private readonly IEmailReclassificationTrigger _reclassificationTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSpamRuleCommandHandler(
        ISpamRuleRepository repository,
        IEmailReclassificationTrigger reclassificationTrigger,
        IUnitOfWork unitOfWork,
        ILogger<DeleteSpamRuleCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _reclassificationTrigger = reclassificationTrigger;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteSpamRuleCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.DeleteAsync(request.Id);
            await _unitOfWork.CompleteAsync();

            _reclassificationTrigger.TriggerReclassification();

            return true;
        }, nameof(DeleteSpamRuleCommand), new { request.Id });
    }
}
