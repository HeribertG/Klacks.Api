# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# CI-only check of a fresh native installation (never copied into the office-PC package): the instance runs as a
# customer installation and not as the Playground, its store address is a public HTTPS URL (in klacks.native.json and in
# the shipped appsettings.Production.json, which installations from before the pin get through auto-update), and the
# database holds no demo or test data. Throws on the first violation. Run elevated (the configuration and secrets are admin-only).
#
# Parameters:
#   BaseUrl       Loopback address of the installed Klacks, e.g. http://127.0.0.1:5180
#   ProgramRoot   Program folder of the installation (holds pgsql\bin\psql.exe)
#   DataRoot      Data folder of the installation (holds config\klacks.native.json and config\secrets.json)
#   PostgresPort  Port of the bundled PostgreSQL service
#
# Example:
#   .\Assert-KlacksCustomerDefaults.ps1 -BaseUrl http://127.0.0.1:5180

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [string]$ProgramRoot = (Join-Path $env:ProgramFiles 'Klacks'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'Klacks'),
    [int]$PostgresPort = 5439
)

$ErrorActionPreference = 'Stop'

$PostgresUser = 'klacks'
$DatabaseName = 'Klacks'
$PlaygroundStatusPath = '/api/backend/Playground/Status'
$PublicUrlPattern = '^https://'
# Filled only by the demo seed (DataSeeder withFake: FakeDataSeed, IdentityProvidersSeed); a fresh customer database
# has no row in any of them, with or without a region profile.
$DemoOnlyTables = @('client', 'address', 'membership', 'annotation', 'client_contract', 'shift', 'work', 'identity_providers')

$configFile = Join-Path $DataRoot 'config\klacks.native.json'
$secretsFile = Join-Path $DataRoot 'config\secrets.json'
$config = Get-Content -Raw $configFile | ConvertFrom-Json

if ($config.Deployment.IsPlayground -ne $false) {
    throw "klacks.native.json does not pin Deployment.IsPlayground to false (found '$($config.Deployment.IsPlayground)')."
}
$playground = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + $PlaygroundStatusPath)
if ($playground.limitActive -ne $false) {
    throw "Playground/Status reports limitActive='$($playground.limitActive)'; the Playground restrictions are active."
}
$marketplaceUrl = [string]$config.LanguagePlugins.MarketplaceUrl
if ($marketplaceUrl -notmatch $PublicUrlPattern) {
    throw "klacks.native.json has no public HTTPS store address in LanguagePlugins.MarketplaceUrl (found '$marketplaceUrl')."
}
$productionSettingsFile = Join-Path $ProgramRoot 'current\app\appsettings.Production.json'
$shippedMarketplaceUrl = [string](Get-Content -Raw $productionSettingsFile | ConvertFrom-Json).LanguagePlugins.MarketplaceUrl
if ($shippedMarketplaceUrl -notmatch $PublicUrlPattern) {
    throw "The shipped appsettings.Production.json has no public HTTPS store address (found '$shippedMarketplaceUrl')."
}

$psql = Join-Path $ProgramRoot 'pgsql\bin\psql.exe'
# The query goes through a file: Windows PowerShell 5.1 strips the double quotes of native arguments.
$queryFile = Join-Path $env:TEMP 'klacks-customer-defaults.sql'
$selects = $DemoOnlyTables | ForEach-Object { "select '$_' || '=' || count(1) from public.$_" }
Set-Content -Path $queryFile -Encoding ASCII -Value (($selects -join ' union all ') + ';')
$env:PGPASSWORD = (Get-Content -Raw $secretsFile | ConvertFrom-Json).DatabasePassword
try {
    $rows = & $psql -h localhost -p $PostgresPort -U $PostgresUser -d $DatabaseName -At -f $queryFile
    if ($LASTEXITCODE -ne 0) { throw "psql exited with $LASTEXITCODE." }
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item $queryFile -ErrorAction SilentlyContinue
}

$rows = @($rows)
if ($rows.Count -ne $DemoOnlyTables.Count) {
    throw "Expected $($DemoOnlyTables.Count) table counts from psql, got: $($rows -join ', ')."
}
$filled = @($rows | Where-Object { $_ -notmatch '=0$' })
if ($filled.Count -gt 0) {
    throw "The fresh database contains demo or test data: $($filled -join ', ')."
}
Write-Host "Customer defaults OK: no Playground mode, store $marketplaceUrl, demo tables empty ($($rows -join ', '))."
