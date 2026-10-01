// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Moves one received email to another folder, locally and on the IMAP server. An email whose sender
/// belongs only to clients outside the caller's group visibility is refused exactly like a missing email.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own the sender address</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see those clients</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class MoveEmailToFolderCommandHandler : BaseHandler, IRequestHandler<MoveEmailToFolderCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImapEmailService _imapService;

    public MoveEmailToFolderCommandHandler(
        IReceivedEmailRepository repository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        IImapEmailService imapService,
        ILogger<MoveEmailToFolderCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
        _imapService = imapService;
    }

    public async Task<bool> Handle(MoveEmailToFolderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null
                || await ReceivedEmailVisibility.IsHiddenAsync(_emailQueryRepository, _clientVisibilityGuard, email, cancellationToken))
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            var previousFolder = email.Folder;

            await _repository.MoveToFolderAsync(request.Id, request.Folder);
            await _unitOfWork.CompleteAsync();

            await _imapService.MoveEmailOnImapAsync(email.ImapUid, previousFolder, request.Folder, cancellationToken);

            return true;
        }, nameof(MoveEmailToFolderCommand), new { request.Id, request.Folder });
    }
}
