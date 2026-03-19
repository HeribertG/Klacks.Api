// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Email;

public record FetchEmailsCommand : IRequest<FetchEmailsResult>;

/// <summary>
/// Ergebnis des IMAP-Email-Abrufs.
/// </summary>
/// <param name="Success">Ob der Abruf erfolgreich war</param>
/// <param name="FetchedCount">Anzahl der neu abgerufenen Emails</param>
/// <param name="Error">Fehlermeldung bei Misserfolg</param>
public record FetchEmailsResult(bool Success, int FetchedCount, string? Error = null);
