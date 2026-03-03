// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class CreateEmailFolderCommandHandler : BaseHandler, IRequestHandler<CreateEmailFolderCommand, EmailFolderResource>
{
    private readonly IEmailFolderRepository _folderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EmailFolderMapper _mapper = new();

    public CreateEmailFolderCommandHandler(
        IEmailFolderRepository folderRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateEmailFolderCommandHandler> logger) : base(logger)
    {
        _folderRepository = folderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailFolderResource> Handle(CreateEmailFolderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var exists = await _folderRepository.ExistsByImapNameAsync(request.ImapFolderName);
            if (exists)
            {
                throw new InvalidOperationException($"A folder with IMAP name '{request.ImapFolderName}' already exists.");
            }

            var allFolders = await _folderRepository.GetAllAsync();
            var maxSortOrder = allFolders.Count > 0 ? allFolders.Max(f => f.SortOrder) : -1;

            var folder = new EmailFolder
            {
                Name = request.Name,
                ImapFolderName = request.ImapFolderName,
                SortOrder = maxSortOrder + 1,
                IsSystem = false
            };

            await _folderRepository.AddAsync(folder);
            await _unitOfWork.CompleteAsync();

            return _mapper.ToResource(folder);
        }, nameof(CreateEmailFolderCommand));
    }
}
