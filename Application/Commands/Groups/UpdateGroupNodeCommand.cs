﻿using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Commands.Groups;

public record UpdateGroupNodeCommand(Guid Id, GroupResource Group) : IRequest<GroupResource>;
