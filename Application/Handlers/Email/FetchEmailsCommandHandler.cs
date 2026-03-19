// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler fuer den manuellen IMAP-Email-Abruf (Folder-Sync + neue Emails fetchen).
/// </summary>
/// <param name="imapEmailService">IMAP-Service fuer Folder-Sync und Email-Abruf</param>
/// <param name="unitOfWork">Persistiert die neuen Emails</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class FetchEmailsCommandHandler : BaseHandler, IRequestHandler<FetchEmailsCommand, FetchEmailsResult>
{
    private readonly IImapEmailService _imapEmailService;
    private readonly IUnitOfWork _unitOfWork;

    public FetchEmailsCommandHandler(
        IImapEmailService imapEmailService,
        IUnitOfWork unitOfWork,
        ILogger<FetchEmailsCommandHandler> logger)
        : base(logger)
    {
        _imapEmailService = imapEmailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<FetchEmailsResult> Handle(FetchEmailsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _imapEmailService.SyncFoldersAsync();
            await _unitOfWork.CompleteAsync();

            var newEmails = await _imapEmailService.FetchNewEmailsAsync();
            if (newEmails.Count > 0)
            {
                await _unitOfWork.CompleteAsync();
            }

            return new FetchEmailsResult(true, newEmails.Count);
        }, nameof(FetchEmailsCommand), null);
    }
}
