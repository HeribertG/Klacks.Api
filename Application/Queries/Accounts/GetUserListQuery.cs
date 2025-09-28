using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;
using System.Collections.Generic;

namespace Klacks.Api.Application.Queries.Accounts;

public record GetUserListQuery() : IRequest<List<UserResource>>;