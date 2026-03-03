// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class MoveEmailToFolderCommandHandler : BaseHandler, IRequestHandler<MoveEmailToFolderCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public MoveEmailToFolderCommandHandler(
        IReceivedEmailRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<MoveEmailToFolderCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MoveEmailToFolderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _repository.MoveToFolderAsync(request.Id, request.Folder);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(MoveEmailToFolderCommand), new { request.Id, request.Folder });
    }
}
