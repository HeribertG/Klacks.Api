using MediatR;

namespace Klacks.Api.Queries;

public record GetQuery<TModel>(Guid Id) : IRequest<TModel>;
