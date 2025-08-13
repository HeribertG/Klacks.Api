using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;

namespace Klacks.Api.Application.Queries.Accounts;

// Login Query
public record LoginUserQuery(string Email, string Password) : IRequest<TokenResource?>;

// Refresh Token Query
public record RefreshTokenQuery(RefreshRequestResource RefreshRequest) : IRequest<TokenResource?>;

// Get User List Query
public record GetUserListQuery() : IRequest<List<UserResource>>;