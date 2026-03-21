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
            var allFolders = await _folderRepository.GetAllAsync();
            var folders = allFolders.Where(f => !string.IsNullOrEmpty(f.SpecialUse) || !f.IsSystem).ToList();
            var folderCounts = await _emailRepository.GetAllFolderCountsAsync();

            var resources = folders.Select(folder =>
            {
                var resource = _mapper.ToResource(folder);
                if (folderCounts.TryGetValue(folder.ImapFolderName, out var counts))
                {
                    resource.UnreadCount = counts.Unread;
                    resource.TotalCount = counts.Total;
                }
                return resource;
            }).ToList();

            return resources;
        }, nameof(GetEmailFoldersQuery));
    }
}
