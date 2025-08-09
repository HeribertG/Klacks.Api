using MediatR;

namespace Klacks.Api.Application.Queries.Clients;

public record CountQuery() : IRequest<int>;
