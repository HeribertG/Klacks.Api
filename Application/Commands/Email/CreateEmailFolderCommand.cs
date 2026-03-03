// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Email;

public record CreateEmailFolderCommand(string Name, string ImapFolderName) : IRequest<EmailFolderResource>;
