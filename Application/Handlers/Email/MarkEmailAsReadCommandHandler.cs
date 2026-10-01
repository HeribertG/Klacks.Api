// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets the read flag of one received email locally and on the IMAP server. An email whose sender belongs
/// only to clients outside the caller's group visibility is refused exactly like a missing email.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own the sender address</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see those clients</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;
using IEmailNotificationService = Klacks.Api.Domain.Interfaces.Email.IEmailNotificationService;

namespace Klacks.Api.Application.Handlers.Email;

public class MarkEmailAsReadCommandHandler : BaseHandler, IRequestHandler<MarkEmailAsReadCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationService _notificationService;
    private readonly IImapEmailService _imapService;

    public MarkEmailAsReadCommandHandler(
        IReceivedEmailRepository repository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        IEmailNotificationService notificationService,
        IImapEmailService imapService,
        ILogger<MarkEmailAsReadCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _imapService = imapService;
    }

    public async Task<bool> Handle(MarkEmailAsReadCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null
                || await ReceivedEmailVisibility.IsHiddenAsync(_emailQueryRepository, _clientVisibilityGuard, email, cancellationToken))
            {
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            }

            email.IsRead = request.IsRead;
            await _repository.UpdateAsync(email);
            await _unitOfWork.CompleteAsync();

            await _imapService.SetReadFlagOnImapAsync(email.ImapUid, email.Folder, request.IsRead, cancellationToken);

            await _notificationService.NotifyReadStateChangedAsync(request.UserId, request.Id, request.IsRead, email.Folder);

            return true;
        }, nameof(MarkEmailAsReadCommand), new { request.Id, request.IsRead });
    }
}
