using MediatR;

namespace Klacks.Api.Queries.Clients;

public record CountQuery() : IRequest<int>;
