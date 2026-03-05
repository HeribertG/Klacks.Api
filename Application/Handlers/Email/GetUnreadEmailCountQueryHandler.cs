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
            var inboxFolder = await _folderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
            if (string.IsNullOrEmpty(inboxFolder))
                return 0;

            return await _repository.GetUnreadCountByFolderAsync(inboxFolder);
        }, nameof(GetUnreadEmailCountQuery));
    }
}
