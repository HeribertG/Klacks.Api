// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Singleton cache of the pseudonym dictionary. Every call reads a cheap fingerprint of clients, communications,
/// addresses and users (no time-to-live: a name entered a second ago must not leave in plain text) and rebuilds the dictionary when it
/// changed. Database access goes through a fresh scope per call, never a captured DbContext; rebuilding is serialised
/// within the process.
/// </summary>
/// <param name="scopeFactory">Creates the scope the repository is resolved from</param>

using System.Diagnostics;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Pseudonymization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Pseudonymization;

public sealed class PseudonymMapSource : IPseudonymMapSource
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PseudonymMapSource> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile CachedDictionary? _cached;

    public PseudonymMapSource(IServiceScopeFactory scopeFactory, ILogger<PseudonymMapSource> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<PseudonymDictionary> GetDictionaryAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILLMPseudonymRepository>();
        var signature = await repository.GetPersonDataSignatureAsync(cancellationToken);
        var cached = _cached;
        if (cached != null && cached.Signature == signature)
        {
            return cached.Dictionary;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            cached = _cached;
            if (cached != null && cached.Signature == signature)
            {
                return cached.Dictionary;
            }

            var stopwatch = Stopwatch.StartNew();
            var persons = await repository.LoadPersonsAsync(cancellationToken);
            var phones = await repository.LoadPhoneNumbersAsync(cancellationToken);
            var addresses = await repository.LoadAddressesAsync(cancellationToken);
            var vocabulary = PseudonymVocabulary.Create(persons, phones, addresses);
            var ordinals = await repository.EnsureOrdinalsAsync(vocabulary.RequiredKeys, cancellationToken);
            var dictionary = vocabulary.Bind(ordinals);
            _cached = new CachedDictionary(signature, dictionary);
            _logger.LogInformation(
                "Pseudonym dictionary built: {PersonCount} people, {PhraseCount} name phrases, {StreetCount} street keys in {ElapsedMs} ms",
                persons.Count, dictionary.PhraseCount, dictionary.StreetCount, stopwatch.ElapsedMilliseconds);
            return dictionary;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed record CachedDictionary(string Signature, PseudonymDictionary Dictionary);
}
