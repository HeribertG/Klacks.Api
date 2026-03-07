// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;
using IEmailNotificationService = Klacks.Api.Domain.Interfaces.Email.IEmailNotificationService;

namespace Klacks.Api.Application.Handlers.Email;

public class MarkEmailAsReadCommandHandler : BaseHandler, IRequestHandler<MarkEmailAsReadCommand, bool>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationService _notificationService;
    private readonly IImapEmailService _imapService;

    public MarkEmailAsReadCommandHandler(
        IReceivedEmailRepository repository,
        IUnitOfWork unitOfWork,
        IEmailNotificationService notificationService,
        IImapEmailService imapService,
        ILogger<MarkEmailAsReadCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _imapService = imapService;
    }

    public async Task<bool> Handle(MarkEmailAsReadCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null)
            {
                throw new KeyNotFoundException($"Email with id {request.Id} not found.");
            }

            email.IsRead = request.IsRead;
            await _repository.UpdateAsync(email);
            await _unitOfWork.CompleteAsync();

            await _imapService.SetReadFlagOnImapAsync(email.ImapUid, email.Folder, request.IsRead, cancellationToken);

            await _notificationService.NotifyReadStateChangedAsync(request.Id, request.IsRead, email.Folder);

            return true;
        }, nameof(MarkEmailAsReadCommand), new { request.Id, request.IsRead });
    }
}
