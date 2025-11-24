using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Branch;

public record DeleteCommand(Guid id) : IRequest;
