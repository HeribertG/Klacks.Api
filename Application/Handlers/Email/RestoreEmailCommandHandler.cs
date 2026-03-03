// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class RestoreEmailCommandHandler : BaseHandler, IRequestHandler<RestoreEmailCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreEmailCommandHandler(
        IReceivedEmailRepository repository,
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork,
        ILogger<RestoreEmailCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RestoreEmailCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var folders = await _folderRepository.GetAllAsync();
            var defaultFolder = folders.FirstOrDefault(f => f.ImapFolderName != EmailConstants.TrashFolder);
            var targetFolder = defaultFolder?.ImapFolderName ?? "INBOX";

            await _repository.MoveToFolderAsync(request.Id, targetFolder);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(RestoreEmailCommand), new { request.Id });
    }
}
