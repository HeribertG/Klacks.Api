// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetEmailFoldersQueryHandler : BaseHandler, IRequestHandler<GetEmailFoldersQuery, List<EmailFolderResource>>
{
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IReceivedEmailRepository _emailRepository;
    private readonly EmailFolderMapper _mapper = new();

    public GetEmailFoldersQueryHandler(
        IEmailFolderRepository folderRepository,
        IReceivedEmailRepository emailRepository,
        ILogger<GetEmailFoldersQueryHandler> logger) : base(logger)
    {
        _folderRepository = folderRepository;
        _emailRepository = emailRepository;
    }

    public async Task<List<EmailFolderResource>> Handle(GetEmailFoldersQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var folders = await _folderRepository.GetAllAsync();
            var resources = new List<EmailFolderResource>();

            foreach (var folder in folders)
            {
                var resource = _mapper.ToResource(folder);
                resource.UnreadCount = await _emailRepository.GetUnreadCountByFolderAsync(folder.ImapFolderName);
                resource.TotalCount = await _emailRepository.GetTotalCountByFolderAsync(folder.ImapFolderName);
                resources.Add(resource);
            }

            return resources;
        }, nameof(GetEmailFoldersQuery));
    }
}
