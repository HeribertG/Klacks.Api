using MediatR;

namespace Klacks_api.Queries;

public record GetQuery<TModel>(Guid Id) : IRequest<TModel>;
