using MediatR;

namespace Klacks_api.Queries.Clients;

public record CountQuery() : IRequest<int>;
