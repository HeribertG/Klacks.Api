// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class DeleteEmailFolderCommandHandler : BaseHandler, IRequestHandler<DeleteEmailFolderCommand, bool>
{
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IReceivedEmailRepository _emailRepository;
    private readonly IImapEmailService _imapEmailService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmailFolderCommandHandler(
        IEmailFolderRepository folderRepository,
        IReceivedEmailRepository emailRepository,
        IImapEmailService imapEmailService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteEmailFolderCommandHandler> logger) : base(logger)
    {
        _folderRepository = folderRepository;
        _emailRepository = emailRepository;
        _imapEmailService = imapEmailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteEmailFolderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var folder = await _folderRepository.GetByIdAsync(request.Id);
            if (folder == null)
            {
                return false;
            }

            if (folder.IsSystem)
            {
                throw new InvalidOperationException("System folders cannot be deleted.");
            }

            var trashFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Trash);
            if (!string.IsNullOrEmpty(trashFolder))
            {
                await _emailRepository.BulkMoveFolderAsync(folder.ImapFolderName, trashFolder);
            }

            await _imapEmailService.DeleteFolderOnImapAsync(folder.ImapFolderName, cancellationToken);

            await _folderRepository.DeleteAsync(request.Id);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(DeleteEmailFolderCommand));
    }
}
