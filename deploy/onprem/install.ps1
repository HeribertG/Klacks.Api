# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Klacks On-Prem (Docker) bootstrap for Windows. Idempotent: first run generates secrets +
# a self-signed certificate and pins the released version; re-runs preserve existing secrets
# and certificate and just pull + (re)start the stack (use it to repair or to force a pull).
#
# Usage:   powershell -ExecutionPolicy Bypass -File .\install.ps1 [-ServerName host] [-HttpPort 80] [-HttpsPort 443] [-Region de]
#          -Region: country/region setup (see regions/README.md), must match a regions/<code>.json
#          file; omit to skip region setup.
#          -ApiMemoryLimit 2560M -IdleUnloadMinutes 30: only for a Docker VM below ~8 GB, always
#          both together (see .env.example); they are written to .env so the updater keeps them.
[CmdletBinding()]
param(
    [string]$ServerName,
    [int]$HttpPort = 80,
    [int]$HttpsPort = 443,
    [string]$Region,
    [string]$ApiMemoryLimit,
    [string]$IdleUnloadMinutes,
    # Optional: only needed while the ghcr packages are private.
    [string]$GhcrUser,
    [string]$GhcrToken
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "!!  $msg" -ForegroundColor Yellow }
function Stop-Install($msg) { Write-Host "XX  $msg" -ForegroundColor Red; exit 1 }

$DockerDesktopUrl = 'https://docs.docker.com/desktop/setup/install/windows-install/'
$BytesPerGiB = 1073741824
# The compose limits add up to 6.25 GiB (postgres 2 GiB, klacks-api 3.5 GiB, ui/updater/nginx
# 0.25 GiB each). Docker Desktop gives its VM half of the PC memory by default, so a 16 GB PC
# reports about 7.7 GiB; 7 GiB is the floor that still leaves the VM some headroom.
$MinDockerMemoryGiB = 7

# Runs a native command without letting $ErrorActionPreference turn its stderr into an exception:
# Windows PowerShell 5.1 aborts on the first stderr line (docker's "Unable to find image" while
# pulling, openssl's key-generation progress). Returns the combined output and leaves the exit
# code in $LASTEXITCODE.
function Invoke-NativeQuiet([string]$command, [string[]]$arguments) {
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $command @arguments 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
        } | Out-String
    }
    finally { $ErrorActionPreference = $previousPreference }
    return $output
}
function Invoke-DockerQuiet([string[]]$arguments) { return Invoke-NativeQuiet 'docker' $arguments }

# --- 1. Prerequisites --------------------------------------------------------
Write-Step "Checking Docker..."
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Stop-Install "Docker Desktop is not installed. Install it ($DockerDesktopUrl), start it once, then re-run this script."
}
$dockerOutput = Invoke-DockerQuiet @('version', '--format', '{{.Server.Version}}')
if ($LASTEXITCODE -ne 0) {
    Stop-Install "Docker Desktop is installed but not running. Start Docker Desktop, wait until it reports 'Engine running', then re-run this script. Docker said: $($dockerOutput.Trim())"
}
Invoke-DockerQuiet @('compose', 'version') | Out-Null
if ($LASTEXITCODE -ne 0) {
    Stop-Install "The Docker Compose plugin ('docker compose') is missing. Update Docker Desktop ($DockerDesktopUrl)."
}

# Automatic updates recreate the containers with bind mounts into this folder, so it has to stay.
if ($PSScriptRoot -match '\\Downloads(\\|$)') {
    Write-Warn "Klacks is installed in $PSScriptRoot. Automatic updates keep using this folder - do not delete it. Better: move the folder to a permanent place (e.g. C:\klacks) and run the installer there."
}

# --- 2. Load / create .env (preserve existing secrets) -----------------------
$envPath = Join-Path $PSScriptRoot '.env'
$envMap = [ordered]@{}
if (Test-Path $envPath) {
    Write-Step "Existing .env found - preserving secrets."
    foreach ($line in Get-Content $envPath) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)=(.*)$') { $envMap[$Matches[1]] = $Matches[2] }
    }
} else {
    Write-Step "First run - generating a fresh .env."
}
$firstRun = -not (Test-Path $envPath)

# Memory overrides go into .env: the updater recreates the containers from .env and never sees this shell.
if ($ApiMemoryLimit) { $envMap['KLACKS_API_MEMORY_LIMIT'] = $ApiMemoryLimit }
if ($IdleUnloadMinutes) { $envMap['KLACKS_API_IDLE_UNLOAD_MINUTES'] = $IdleUnloadMinutes }
if ([bool]$envMap['KLACKS_API_MEMORY_LIMIT'] -ne [bool]$envMap['KLACKS_API_IDLE_UNLOAD_MINUTES']) {
    Stop-Install "Set -ApiMemoryLimit and -IdleUnloadMinutes together - a lowered limit without the idle unload gets the API killed (see .env.example)."
}

# Below the floor a first install stops unless the API limit is lowered; a re-run only warns, so an
# existing small installation can still be updated by re-running the script.
Write-Step "Checking memory..."
$memoryOutput = (Invoke-DockerQuiet @('info', '--format', '{{.MemTotal}}')).Trim()
[long]$dockerMemory = 0
if (($LASTEXITCODE -ne 0) -or -not [long]::TryParse($memoryOutput, [ref]$dockerMemory) -or $dockerMemory -le 0) {
    Write-Warn "Could not read the memory available to Docker; skipping the memory check."
} elseif ($dockerMemory -lt ($MinDockerMemoryGiB * $BytesPerGiB)) {
    $gib = [math]::Round($dockerMemory / $BytesPerGiB, 1)
    if ($envMap['KLACKS_API_MEMORY_LIMIT']) {
        Write-Warn "Docker sees $gib GiB of memory; running with the reduced API limit $($envMap['KLACKS_API_MEMORY_LIMIT'])."
    } else {
        $memoryMessage = "Docker sees only $gib GiB of memory, Klacks needs at least $MinDockerMemoryGiB GiB: the containers may use up to 6.25 GiB. Give Docker more memory (Docker Desktop uses half of the PC memory; a 16 GB PC is recommended, the WSL limit is set in %UserProfile%\.wslconfig), or lower the API limit together with the idle unload: -ApiMemoryLimit 2560M -IdleUnloadMinutes 30 (see .env.example)."
        if ($firstRun) { Stop-Install $memoryMessage }
        Write-Warn $memoryMessage
    }
}

function New-Secret([int]$bytes = 48) {
    $buf = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($buf)
    return [Convert]::ToBase64String($buf)
}
function Set-IfEmpty($key, $value) { if (-not $envMap[$key]) { $envMap[$key] = $value } }
function Set-IfAbsent($key, $value) { if (-not $envMap.Contains($key)) { $envMap[$key] = $value } }

# Certificate that wraps the DataProtection key ring. The ring itself lives in the database, so a
# pg_dump keeps it current even though it rotates every 90 days; this certificate is what stops
# that dump from also containing the key which opens every stored secret. Generated once and kept
# in .env, which has to be backed up anyway for POSTGRES_PASSWORD - so it costs no extra chore.
function New-DataProtectionCertificate($password) {
    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        'CN=Klacks DataProtection',
        $rsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $cert = $request.CreateSelfSigned(
        [DateTimeOffset]::UtcNow.AddDays(-1),
        [DateTimeOffset]::UtcNow.AddYears(10))
    $pfx = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
    return [Convert]::ToBase64String($pfx)
}

Set-IfEmpty 'COMPOSE_PROJECT_NAME' 'klacks'
Set-IfEmpty 'POSTGRES_PASSWORD' (New-Secret)
Set-IfEmpty 'JWT_SECRET' (New-Secret 64)

if (-not $envMap['DATAPROTECTION_CERT_BASE64']) {
    Write-Step 'Generating the DataProtection certificate...'
    try {
        $dpPassword = New-Secret 24
        $envMap['DATAPROTECTION_CERT_BASE64'] = New-DataProtectionCertificate $dpPassword
        $envMap['DATAPROTECTION_CERT_PASSWORD'] = $dpPassword
        Write-Warn 'DataProtection certificate created. Keep .env safe - without it every stored password and API key is unrecoverable.'
    }
    catch {
        Write-Warn "Could not generate the DataProtection certificate ($($_.Exception.Message)); the key ring stays on the api-dataprotection volume, which must then be backed up continuously."
    }
}
Set-IfEmpty 'KLACKS_UPDATER_TAG' 'latest'
Set-IfEmpty 'UPDATE_MANIFEST_BASE_URL' 'https://github.com/HeribertG/Klacks.Api/releases/latest/download'
# Geo services: traffic stays on by default, see README.md for the no-outbound mode. The CSP key is
# only filled when absent, because an empty value is a deliberate operator choice (no tile host).
Set-IfEmpty 'GEO_NO_OUTBOUND_TRAFFIC' 'false'
Set-IfAbsent 'MAP_TILE_CSP_SOURCES' 'https://tile.openstreetmap.org https://*.tile.openstreetmap.org'

if ($ServerName) { $envMap['SERVER_NAME'] = $ServerName }
Set-IfEmpty 'SERVER_NAME' 'localhost'
$envMap['HTTP_PORT'] = "$HttpPort"
$envMap['HTTPS_PORT'] = "$HttpsPort"
$serverName = $envMap['SERVER_NAME']

if ($Region) {
    $regionLower = $Region.ToLowerInvariant()
    $regionFile = Join-Path $PSScriptRoot "regions\$regionLower.json"
    if (-not (Test-Path $regionFile)) { Write-Warn "regions\$regionLower.json not found - aborting."; exit 1 }
    $envMap['REGION_SETUP_FILE'] = "/app/regions/$regionLower.json"
    Write-Step "Region setup: $regionLower (applied once on first boot)."
}

# --- 3. Vendor trust root (signature public key) -----------------------------
# Ships in the bundle as update-public-key.pem. Stored single-line with literal \n
# (docker compose expands it back to real newlines for the updater).
if (-not $envMap['UPDATE_SIGNATURE_PUBLIC_KEY']) {
    $pubPath = Join-Path $PSScriptRoot 'update-public-key.pem'
    if (Test-Path $pubPath) {
        $pem = (Get-Content $pubPath -Raw).Replace("`r`n", "`n").TrimEnd("`n")
        $envMap['UPDATE_SIGNATURE_PUBLIC_KEY'] = '"' + $pem.Replace("`n", '\n') + '"'
    } else {
        Write-Warn "update-public-key.pem missing - auto-update signature verification cannot run until UPDATE_SIGNATURE_PUBLIC_KEY is set."
    }
}

# --- 4. Pin the released version from the signed manifest --------------------
if (-not $envMap['KLACKS_API_TAG'] -or $envMap['KLACKS_API_TAG'] -eq 'latest') {
    try {
        Write-Step "Resolving latest released version from the manifest..."
        $manifestUrl = ($envMap['UPDATE_MANIFEST_BASE_URL'].TrimEnd('/')) + '/stable.json'
        $manifest = Invoke-RestMethod -Uri $manifestUrl -TimeoutSec 30
        $ver = $manifest.latestVersion
        if ($ver) {
            $envMap['KLACKS_API_TAG'] = $ver
            $envMap['KLACKS_UI_TAG'] = $ver
            Write-Step "Pinned api + ui to $ver."
        }
    } catch {
        Write-Warn "Could not fetch manifest ($_). Falling back to :latest tags."
        Set-IfEmpty 'KLACKS_API_TAG' 'latest'
        Set-IfEmpty 'KLACKS_UI_TAG' 'latest'
    }
}

# --- 4b. Compose project name + host path for the updater --------------------
# The updater drives compose from inside its container. The Docker daemon resolves the relative
# bind mounts (./regions, ./init-scripts) against the project directory as the updater sees it, so
# that directory must be mounted at the path under which the Docker Desktop VM reaches this folder
# (install.sh uses the identical host path on Linux). A container-only path such as /apps points
# the mounts at empty directories (production incident 2026-09-10).
$envMap['KLACKS_INSTALL_DIR'] = $PSScriptRoot

$updaterProjectDir = $null
if ($PSScriptRoot -match '^([A-Za-z]):\\(.*)$') {
    $drive = $Matches[1].ToLowerInvariant()
    $rest = $Matches[2] -replace '\\', '/'
    $candidates = @("/run/desktop/mnt/host/$drive/$rest", "/host_mnt/$drive/$rest")
    foreach ($candidate in $candidates) {
        Invoke-DockerQuiet @('run', '--rm', '-v', "${candidate}:/probe:ro", 'nginx:alpine', 'test', '-f', '/probe/docker-compose.yml') | Out-Null
        if ($LASTEXITCODE -eq 0) { $updaterProjectDir = $candidate; break }
    }
}
if ($updaterProjectDir) {
    $envMap['KLACKS_UPDATER_PROJECT_DIR'] = $updaterProjectDir
    Write-Step "Updater project path: $updaterProjectDir"
} else {
    $envMap.Remove('KLACKS_UPDATER_PROJECT_DIR')
    Write-Warn "Could not map this folder into the Docker VM; automatic updates will not work. Install Klacks on a local drive (e.g. C:\klacks) and re-run this script."
}

# --- 5. Write .env -----------------------------------------------------------
$lines = $envMap.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }
Set-Content -Path $envPath -Value $lines -Encoding ascii
Write-Step ".env written."

# --- 6. TLS certificate (self-signed if none provided) -----------------------
$certDir = Join-Path $PSScriptRoot 'nginx\certs'
New-Item -ItemType Directory -Force -Path $certDir | Out-Null
$crt = Join-Path $certDir 'server.crt'
$key = Join-Path $certDir 'server.key'
if ((Test-Path $crt) -and (Test-Path $key)) {
    Write-Step "Reusing existing certificate in nginx\certs."
} else {
    Write-Step "Generating a self-signed certificate for $serverName..."
    $subj = "/CN=$serverName"
    $openssl = Get-Command openssl -ErrorAction SilentlyContinue
    if ($openssl) {
        # A local openssl can be unusable (e.g. OPENSSL_CONF pointing at another product's missing
        # config), so its result is checked and the container below is the fallback.
        Invoke-NativeQuiet 'openssl' @('req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '825', '-keyout', $key, '-out', $crt, '-subj', $subj) | Out-Null
    }
    if (-not ((Test-Path $crt) -and (Test-Path $key))) {
        # No usable local openssl: run it inside a throwaway container (Docker is already required).
        Invoke-DockerQuiet @('run', '--rm', '-v', "${certDir}:/certs", 'alpine/openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '825',
            '-keyout', '/certs/server.key', '-out', '/certs/server.crt', '-subj', $subj) | Out-Null
    }
    if (-not ((Test-Path $crt) -and (Test-Path $key))) {
        Write-Warn "Could not create the certificate. Place server.crt and server.key in nginx\certs and re-run this script."
        exit 1
    }
    Write-Warn "Self-signed certificate created. Browsers will warn until you install a trusted (BYO) cert into nginx\certs."
}

# --- 7. Optional ghcr login (only if packages are private) -------------------
if ($GhcrUser -and $GhcrToken) {
    Write-Step "Logging in to ghcr.io..."
    $GhcrToken | docker login ghcr.io -u $GhcrUser --password-stdin | Out-Null
}

# --- 8. Pull + start ---------------------------------------------------------
Write-Step "Pulling images..."
docker compose pull
if ($LASTEXITCODE -ne 0) { Stop-Install "Pulling the images failed (see above). Check the internet connection and .env, then re-run this script." }
Write-Step "Starting the stack..."
docker compose up -d
if ($LASTEXITCODE -ne 0) { Stop-Install "Starting the stack failed (see above). Check: docker compose logs" }

# --- 9. Wait for health ------------------------------------------------------
Write-Step "Waiting for the API to become healthy (first run migrates + seeds the database)..."
$healthy = $false
for ($i = 1; $i -le 60; $i++) {
    Start-Sleep -Seconds 10
    $status = (Invoke-DockerQuiet @('inspect', '--format', '{{.State.Health.Status}}', 'klacks-api')).Trim()
    if ($status -eq 'healthy') { $healthy = $true; break }
    Write-Host "   ...still starting ($i/60, status: $status)"
}

Write-Host ""
if ($healthy) {
    $httpsSuffix = if ($envMap['HTTPS_PORT'] -ne '443') { ":$($envMap['HTTPS_PORT'])" } else { '' }
    Write-Host "Klacks is up at https://$serverName$httpsSuffix" -ForegroundColor Green
} else {
    Write-Warn "API did not report healthy yet. Check: docker compose logs klacks-api"
}
Write-Host ""
Write-Host "Default login:  admin@test.com  /  P@ssw0rt1" -ForegroundColor Green
Write-Warn  "SECURITY: change this password immediately after first login, and update the mail/SMTP settings in the admin UI."
