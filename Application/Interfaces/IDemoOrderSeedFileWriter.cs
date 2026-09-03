// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes the generated demo order import document to the application content root, next to the
/// installation rather than into the ERP drop point, so the operator decides when it is imported.
/// </summary>

namespace Klacks.Api.Application.Interfaces;

public interface IDemoOrderSeedFileWriter
{
    /// <summary>
    /// Reads the seeded customers, renders the demo orders and stores the document.
    /// </summary>
    /// <param name="language">Two-letter language code selecting order names and descriptions</param>
    /// <param name="cancellationToken">Token cancelling the customer query</param>
    /// <returns>The absolute path of the written file, or null when no customer was seeded</returns>
    Task<string?> WriteAsync(string language, CancellationToken cancellationToken = default);
}
