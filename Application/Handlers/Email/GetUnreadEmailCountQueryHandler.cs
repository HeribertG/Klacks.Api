// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetUnreadEmailCountQueryHandler : BaseHandler, IRequestHandler<GetUnreadEmailCountQuery, int>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailFolderRepository _folderRepository;

    public GetUnreadEmailCountQueryHandler(
        IReceivedEmailRepository repository,
        IEmailFolderRepository folderRepository,
        ILogger<GetUnreadEmailCountQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _folderRepository = folderRepository;
    }

    public async Task<int> Handle(GetUnreadEmailCountQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var inboxUnread = 0;
            var inboxFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
            if (!string.IsNullOrEmpty(inboxFolder))
            {
                inboxUnread = await _repository.GetUnreadCountByFolderAsync(inboxFolder);
            }

            var groupUnread = await _repository.GetUnreadCountByFolderAsync(EmailConstants.ClientAssignedFolder);

            return inboxUnread + groupUnread;
        }, nameof(GetUnreadEmailCountQuery));
    }
}
