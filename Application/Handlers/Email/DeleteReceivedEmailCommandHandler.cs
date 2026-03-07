// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class DeleteReceivedEmailCommandHandler : BaseHandler, IRequestHandler<DeleteReceivedEmailCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImapEmailService _imapService;

    public DeleteReceivedEmailCommandHandler(
        IReceivedEmailRepository repository,
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork,
        IImapEmailService imapService,
        ILogger<DeleteReceivedEmailCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
        _imapService = imapService;
    }

    public async Task<bool> Handle(DeleteReceivedEmailCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var trashFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Trash);
            if (string.IsNullOrEmpty(trashFolder))
                throw new InvalidOperationException("No trash folder configured.");

            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null)
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            var previousFolder = email.Folder;

            await _repository.MoveToFolderAsync(request.Id, trashFolder);
            await _unitOfWork.CompleteAsync();

            await _imapService.MoveEmailOnImapAsync(email.ImapUid, previousFolder, trashFolder, cancellationToken);

            return true;
        }, nameof(DeleteReceivedEmailCommand), new { request.Id });
    }
}
