// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IImapEmailService
{
    Task<List<ReceivedEmail>> FetchNewEmailsAsync(CancellationToken cancellationToken = default);
    Task SyncFoldersAsync(CancellationToken cancellationToken = default);
    Task SyncEmailStatesAsync(CancellationToken cancellationToken = default);
    Task MoveEmailOnImapAsync(long imapUid, string sourceFolder, string targetFolder, CancellationToken cancellationToken = default);
    Task SetReadFlagOnImapAsync(long imapUid, string folder, bool isRead, CancellationToken cancellationToken = default);
    Task DeleteEmailOnImapAsync(long imapUid, string folder, CancellationToken cancellationToken = default);
    Task CreateFolderOnImapAsync(string folderName, CancellationToken cancellationToken = default);
    Task DeleteFolderOnImapAsync(string folderName, CancellationToken cancellationToken = default);
}
