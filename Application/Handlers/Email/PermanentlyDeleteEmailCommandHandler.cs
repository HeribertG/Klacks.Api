// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Permanently deletes one received email from the trash, locally and on the IMAP server. An email whose
/// sender belongs only to clients outside the caller's group visibility is refused exactly like a missing email.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own the sender address</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see those clients</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class PermanentlyDeleteEmailCommandHandler : BaseHandler, IRequestHandler<PermanentlyDeleteEmailCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImapEmailService _imapService;

    public PermanentlyDeleteEmailCommandHandler(
        IReceivedEmailRepository repository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork,
        IImapEmailService imapService,
        ILogger<PermanentlyDeleteEmailCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
        _imapService = imapService;
    }

    public async Task<bool> Handle(PermanentlyDeleteEmailCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null
                || await ReceivedEmailVisibility.IsHiddenAsync(_emailQueryRepository, _clientVisibilityGuard, email, cancellationToken))
            {
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            }

            var trashFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Trash);
            if (email.Folder != trashFolder)
            {
                throw new InvalidRequestException("Only emails in trash can be permanently deleted.");
            }

            await _repository.DeleteAsync(request.Id);
            await _unitOfWork.CompleteAsync();

            await _imapService.DeleteEmailOnImapAsync(email.ImapUid, trashFolder, cancellationToken);

            return true;
        }, nameof(PermanentlyDeleteEmailCommand), new { request.Id });
    }
}
