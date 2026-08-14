# Copyright (c) Heribert Gasparoli Private. All rights reserved.
#
# Klacks On-Prem (Docker) bootstrap for Windows. Idempotent: first run generates secrets +
# a self-signed certificate and pins the released version; re-runs preserve existing secrets
# and certificate and just pull + (re)start the stack (use it to repair or to force a pull).
#
# Usage:   powershell -ExecutionPolicy Bypass -File .\install.ps1 [-ServerName host] [-HttpPort 80] [-HttpsPort 443] [-Region de]
#          -Region: country/region setup (see regions/README.md), must match a regions/<code>.json
#          file; omit to skip region setup.
[CmdletBinding()]
param(
    [string]$ServerName,
    [int]$HttpPort = 80,
    [int]$HttpsPort = 443,
    [string]$Region,
    # Optional: only needed while the ghcr packages are private.
    [string]$GhcrUser,
    [string]$GhcrToken
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "!!  $msg" -ForegroundColor Yellow }

# --- 1. Prerequisites --------------------------------------------------------
Write-Step "Checking Docker..."
docker version --format '{{.Server.Version}}' | Out-Null
docker compose version | Out-Null

# --- 2. Load / create .env (preserve existing secrets) -----------------------
$envPath = Join-Path $PSScriptRoot '.env'
$envMap = [ordered]@{}
if (Test-Path $envPath) {
    Write-Step "Existing .env found — preserving secrets."
    foreach ($line in Get-Content $envPath) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)=(.*)$') { $envMap[$Matches[1]] = $Matches[2] }
    }
} else {
    Write-Step "First run — generating a fresh .env."
}

function New-Secret([int]$bytes = 48) {
    $buf = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($buf)
    return [Convert]::ToBase64String($buf)
}
function Set-IfEmpty($key, $value) { if (-not $envMap[$key]) { $envMap[$key] = $value } }

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

if ($ServerName) { $envMap['SERVER_NAME'] = $ServerName }
Set-IfEmpty 'SERVER_NAME' 'localhost'
$envMap['HTTP_PORT'] = "$HttpPort"
$envMap['HTTPS_PORT'] = "$HttpsPort"
$serverName = $envMap['SERVER_NAME']

if ($Region) {
    $regionLower = $Region.ToLowerInvariant()
    $regionFile = Join-Path $PSScriptRoot "regions\$regionLower.json"
    if (-not (Test-Path $regionFile)) { Write-Warn "regions\$regionLower.json not found — aborting."; exit 1 }
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
        Write-Warn "update-public-key.pem missing — auto-update signature verification cannot run until UPDATE_SIGNATURE_PUBLIC_KEY is set."
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
        & openssl req -x509 -newkey rsa:2048 -nodes -days 825 -keyout $key -out $crt -subj $subj | Out-Null
    } else {
        # No local openssl: run it inside a throwaway container (Docker is already required).
        docker run --rm -v "${certDir}:/certs" alpine/openssl req -x509 -newkey rsa:2048 -nodes -days 825 `
            -keyout /certs/server.key -out /certs/server.crt -subj $subj | Out-Null
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
Write-Step "Starting the stack..."
docker compose up -d

# --- 9. Wait for health ------------------------------------------------------
Write-Step "Waiting for the API to become healthy (first run migrates + seeds the database)..."
$healthy = $false
for ($i = 1; $i -le 60; $i++) {
    Start-Sleep -Seconds 10
    $status = (docker inspect --format '{{.State.Health.Status}}' klacks-api 2>$null)
    if ($status -eq 'healthy') { $healthy = $true; break }
    Write-Host "   ...still starting ($i/60, status: $status)"
}

Write-Host ""
if ($healthy) {
    Write-Host "Klacks is up at https://$serverName" -ForegroundColor Green
} else {
    Write-Warn "API did not report healthy yet. Check: docker compose logs klacks-api"
}
Write-Host ""
Write-Host "Default login:  admin@test.com  /  P@ssw0rt1" -ForegroundColor Green
Write-Warn  "SECURITY: change this password immediately after first login, and update the mail/SMTP settings in the admin UI."
