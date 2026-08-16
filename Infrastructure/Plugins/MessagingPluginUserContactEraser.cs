// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Erases the messaging plugin's user-level messenger identities during a GDPR account deletion.
/// UserMessengerContact holds a Telegram chat id, a phone number or a comparable identifier and is
/// therefore personal data, but it has no foreign key to AspNetUsers (the plugin owns the table and
/// must stay removable without leaving constraints on the core schema) and it is no DbSet on
/// DataBaseContext - it only enters the model through PluginModelRegistry. Neither the database nor
/// UserDataEraser's curated list would have removed it, so the rows would have outlived the account.
/// Deliberately WITHOUT an IPluginStateChecker gate, unlike MessagingPluginOfflineMessengerNotifier:
/// a switched-off plugin still has its rows in the database, and honouring the switch here would
/// leave exactly the personal data behind that this class exists to remove. The switch decides
/// whether messages go out, never whether an erasure is complete.
/// Uses ExecuteDelete, matching UserDataEraser's semantics: the rows are physically gone, including
/// ones whose IsDeleted flag was already set. IgnoreQueryFilters is defensive rather than load-bearing
/// here - UserMessengerContactConfiguration declares no global query filter today, so nothing would
/// hide a soft-deleted row; the call keeps the erasure complete should a filter be added later.
/// </summary>
/// <param name="context">The application DbContext the plugin's entity is registered on.</param>

using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Messaging.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessagingPluginUserContactEraser : IUserDataErasureParticipant
{
    private const string DataSet = "messaging plugin user messenger contacts";

    private readonly DataBaseContext _context;

    public MessagingPluginUserContactEraser(DataBaseContext context)
    {
        _context = context;
    }

    public string DataSetName => DataSet;

    public async Task<int> EraseAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        return await _context.Set<UserMessengerContact>()
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
