using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;
using System.Collections.Generic;

namespace Klacks.Api.Application.Queries.Accounts;

public record GetUserListQuery() : IRequest<List<UserResource>>;