// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the shared Klacks.Docs documentation to MCP resources: lists every available doc
/// under the 'klacks://docs/{name}' URI scheme and reads its Markdown content on demand.
/// </summary>
/// <param name="uri">Resource URI of the form 'klacks://docs/{docName}' identifying a single doc</param>

using Klacks.Docs;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public class McpResourceCatalog : IMcpResourceCatalog
{
    public IList<Resource> ListResources()
    {
        return DocsReader.GetAvailableDocs()
            .OrderBy(doc => doc.Key, StringComparer.Ordinal)
            .Select(doc => new Resource
            {
                Uri = BuildDocUri(doc.Key),
                Name = doc.Key,
                Description = doc.Value,
                MimeType = McpServerConstants.MarkdownMimeType
            })
            .ToList();
    }

    public async Task<ReadResourceResult?> ReadResourceAsync(string uri)
    {
        var docName = ExtractDocName(uri);
        if (docName is null || !DocsReader.DocExists(docName))
        {
            return null;
        }

        var markdown = await DocsReader.ReadDocAsync(docName);

        return new ReadResourceResult
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = uri,
                    MimeType = McpServerConstants.MarkdownMimeType,
                    Text = markdown
                }
            ]
        };
    }

    private static string BuildDocUri(string docName)
    {
        return $"{McpServerConstants.DocsResourceUriPrefix}{docName}";
    }

    private static string? ExtractDocName(string uri)
    {
        if (string.IsNullOrEmpty(uri)
            || !uri.StartsWith(McpServerConstants.DocsResourceUriPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var docName = uri[McpServerConstants.DocsResourceUriPrefix.Length..];

        return docName.Length == 0 ? null : docName;
    }
}
