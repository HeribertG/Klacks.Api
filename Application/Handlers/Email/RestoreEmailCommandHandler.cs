// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Restores one received email from the trash, locally and on the IMAP server. An email whose sender
/// belongs only to clients outside the caller's group visibility is refused exactly like a missing email.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own the sender address</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see those clients</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class RestoreEmailCommandHandler : BaseHandler, IRequestHandler<RestoreEmailCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImapEmailService _imapService;

    public RestoreEmailCommandHandler(
        IReceivedEmailRepository repository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork,
        IImapEmailService imapService,
        ILogger<RestoreEmailCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
        _imapService = imapService;
    }

    public async Task<bool> Handle(RestoreEmailCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var folders = await _folderRepository.GetAllAsync();
            var defaultFolder = folders.FirstOrDefault(f => f.SpecialUse != FolderSpecialUse.Trash);
            var targetFolder = defaultFolder?.ImapFolderName ?? "INBOX";

            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null
                || await ReceivedEmailVisibility.IsHiddenAsync(_emailQueryRepository, _clientVisibilityGuard, email, cancellationToken))
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            var previousFolder = email.Folder;

            await _repository.MoveToFolderAsync(request.Id, targetFolder);
            await _unitOfWork.CompleteAsync();

            await _imapService.MoveEmailOnImapAsync(email.ImapUid, previousFolder, targetFolder, cancellationToken);

            return true;
        }, nameof(RestoreEmailCommand), new { request.Id });
    }
}
