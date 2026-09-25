// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sends the switch or undo command of the macro assignment skills and turns the expected failures into the text the
/// skill answers with: a refusal of the handler (refused plan, macro that cannot run, holder or switch changed since the
/// plan, read-back mismatch) is relayed verbatim; a database failure at the commit answers with
/// <see cref="MacroAssignmentTextFormatter.SaveFailedMessage"/>, never with the raw database message, and without claiming
/// that nothing changed. Any other exception is left to the skill executor.
/// </summary>
/// <param name="mediator">Dispatches the command</param>
/// <param name="command">The switch or undo command</param>

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

public static class MacroAssignmentCommandSender
{
    public static async Task<(MacroAssignmentOutcome? Outcome, string? Error)> SendAsync(
        IMediator mediator, IRequest<MacroAssignmentOutcome> command, CancellationToken cancellationToken)
    {
        try
        {
            return (await mediator.Send(command, cancellationToken), null);
        }
        catch (InvalidRequestException ex)
        {
            return (null, ex.Message);
        }
        catch (DatabaseUpdateException)
        {
            return (null, MacroAssignmentTextFormatter.SaveFailedMessage);
        }
        catch (ConcurrencyException)
        {
            return (null, MacroAssignmentTextFormatter.SaveFailedMessage);
        }
    }
}
