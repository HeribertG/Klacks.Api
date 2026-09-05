<#
.SYNOPSIS
    Export knowledge index embedding snapshot from a fresh development database.

.DESCRIPTION
    Authenticates to the Klacks API backend and exports the complete knowledge index
    snapshot (text_hash -> embedding vector mappings) to a JSON file for distribution.

    CRITICAL: The snapshot MUST be exported from a freshly seeded database. Learned
    phrases in the development database change the text_hash values (via KnowledgeIndexSynchronizer),
    making the snapshot incompatible with clean deployments. Use a separate fresh database
    (e.g., klacks_snapbase) for export.

    The exported JSON contains every knowledge index entry (skills, recipes) with its
    embedding vector, the embedding space ID and creation metadata.
    On startup, KnowledgeIndexSynchronizer loads this snapshot and skips embedding
    computation for entries whose hashes match, reducing cold-start time significantly.

.PARAMETER BaseUrl
    API base URL. Default: https://localhost:5001. The TLS certificate is only skipped for loopback hosts.

.PARAMETER Email
    Admin account email for authentication. Default: admin@test.com

.PARAMETER Password
    Admin account password. Required; no default. Use securely from secure storage.

.PARAMETER OutFile
    Output file path for the snapshot JSON. Default: <repo>/KnowledgeIndex/Snapshot/knowledge-index-snapshot.json
    Resolved via $PSScriptRoot if relative.

.EXAMPLE
    PS> .\export-knowledge-index-snapshot.ps1 -Password "P@ssw0rt1"
    Authenticates as admin@test.com against https://localhost:5001 and exports to default path.

#>

param(
    [string]$BaseUrl = "https://localhost:5001",
    [string]$Email = "admin@test.com",
    [Parameter(Mandatory = $true)]
    [string]$Password,
    [string]$OutFile = (Join-Path $PSScriptRoot "..\KnowledgeIndex\Snapshot\knowledge-index-snapshot.json")
)

$ErrorActionPreference = 'Stop'

$skipCertificateCheck = ([uri]$BaseUrl).IsLoopback

# Resolve output file path
$OutFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile)

Write-Host "Exporting knowledge index snapshot..."
Write-Host "  Base URL: $BaseUrl"
Write-Host "  Email: $Email"
Write-Host "  Output: $OutFile"

try {
    # Authenticate
    Write-Host "Authenticating..."
    $loginBody = @{
        email    = $Email
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -SkipCertificateCheck:$skipCertificateCheck `
        -Method Post `
        -Uri "$BaseUrl/api/backend/Accounts/LoginUser" `
        -ContentType "application/json" `
        -Body $loginBody

    if (-not $loginResponse.token) {
        throw "Login failed: no token in response"
    }

    $token = $loginResponse.token
    Write-Host "Authentication successful"

    # Export snapshot
    Write-Host "Exporting snapshot from $BaseUrl/api/backend/knowledge-index/snapshot..."
    $headers = @{
        Authorization = "Bearer $token"
    }

    # Ensure output directory exists
    $outDir = Split-Path -Parent $OutFile
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
        Write-Host "Created output directory: $outDir"
    }

    Invoke-WebRequest `
        -SkipCertificateCheck:$skipCertificateCheck `
        -Method Get `
        -Uri "$BaseUrl/api/backend/knowledge-index/snapshot" `
        -Headers $headers `
        -OutFile $OutFile

    # Parse and display summary
    $snapshot = Get-Content $OutFile | ConvertFrom-Json

    $embeddingSpaceId = $snapshot.embeddingSpaceId
    $dimension = $snapshot.dimension
    $entryCount = $snapshot.entries.Count
    $fileSizeKB = [math]::Round((Get-Item $OutFile).Length / 1024, 2)

    Write-Host "Export completed successfully"
    Write-Host "  Embedding Space ID: $embeddingSpaceId"
    Write-Host "  Dimension: $dimension"
    Write-Host "  Entries: $entryCount"
    Write-Host "  File size: $fileSizeKB KB"
    Write-Host "  Output: $OutFile"

}
catch {
    Write-Error "Export failed: $_"
    exit 1
}
