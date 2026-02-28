// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class PermanentlyDeleteEmailCommandHandler : BaseHandler, IRequestHandler<PermanentlyDeleteEmailCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PermanentlyDeleteEmailCommandHandler(
        IReceivedEmailRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PermanentlyDeleteEmailCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(PermanentlyDeleteEmailCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null)
            {
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            }

            if (email.Folder != EmailConstants.TrashFolder)
            {
                throw new InvalidRequestException("Only emails in trash can be permanently deleted.");
            }

            await _repository.DeleteAsync(request.Id);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(PermanentlyDeleteEmailCommand), new { request.Id });
    }
}
