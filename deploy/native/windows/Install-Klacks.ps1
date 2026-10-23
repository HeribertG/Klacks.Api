# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Provisions a native Klacks installation on Windows: PostgreSQL cluster, secrets, configuration file, version layout
# and the three services (KlacksPostgres, Klacks, KlacksUpdater) plus, when the bundle ships it, the speech recognition
# service KlacksStt (registered Disabled; the updater enables and starts it when the user installs the Parakeet engine).
# Re-running it is safe: existing secrets, the database cluster and installed versions are kept, and a half-configured
# KlacksStt registration is completed (same steps and values as the updater's WindowsSttServiceRegistrar).
# The Inno Setup installer only copies the payload and calls this script elevated; everything up to the service
# registration can be run without administrator rights via -NoServices, which starts PostgreSQL and the API as plain
# processes instead (development and CI smoke tests).
#
# Windows PowerShell 5.1 compatible on purpose: it is the only PowerShell every Windows 10/11 machine has.
#
# Layout:  <ProgramRoot>\versions\<x.y.z>\{app,ui,updater,stt}   <ProgramRoot>\current -> junction   <ProgramRoot>\pgsql
#          <DataRoot>\pgdata  <DataRoot>\config\klacks.native.json  <DataRoot>\backups  <DataRoot>\logs
#          <DataRoot>\stt-models
#          (+ API data)

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BundleZip,
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$PostgresDirectory,
    [string]$ProgramRoot = (Join-Path $env:ProgramFiles 'Klacks'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'Klacks'),
    [int]$HttpPort = 5180,
    [int]$PostgresPort = 5439,
    [string]$Region = '',
    [string]$UpdatePublicKeyPath = '',
    [string]$UpdateManifestBaseUrl = 'https://github.com/HeribertG/Klacks.Api/releases/latest/download',
    [string]$VcRedistPath = '',
    [int]$HealthTimeoutSeconds = 1800,
    [switch]$AllowUnsignedUpdates,
    [switch]$NoServices
)

$ErrorActionPreference = 'Stop'

$ApiServiceName = 'Klacks'
$UpdaterServiceName = 'KlacksUpdater'
$SttServiceName = 'KlacksStt'
# Loopback port of the local speech recognition service; must equal the SttBaseUrl the updater writes into the provider
# row (SttServiceRegistration.Port in the updater).
$SttPort = 5091
$SttThreads = 2
$SttModelDirectoryName = 'stt-models'
$PostgresServiceName = 'KlacksPostgres'
# Windows event log sources of the API and the updater (their ApplicationName). Registered at install time, while
# elevated, so the services never have to create them on their first log write.
$EventLogSources = @('Klacks.Api', 'Klacks.Updater')
$EventLogName = 'Application'
$PostgresSuperUser = 'klacks'
$DatabaseName = 'Klacks'
$ConfigFileName = 'klacks.native.json'
# Last status line when Klacks answered its health check; Setup.exe reads it to tell "ready" from "still starting".
$ReadyMarker = 'READY'
$SecretsFileName = 'secrets.json'
$LocalSystemSid = 'S-1-5-18'
$AdministratorsSid = 'S-1-5-32-544'
# pg_ctl register runs PostgreSQL as NT AUTHORITY\NetworkService unless an account is given.
$NetworkServiceSid = 'S-1-5-20'
$AuthenticatedUsersSid = 'S-1-5-11'
# Host filtering against DNS rebinding: the browser only ever reaches the PC itself (LAN access is Phase 6b).
$AllowedHosts = 'localhost;127.0.0.1'
# Same as the Docker image: the database never follows the PC's zone, the company zone lives in Klacks.
$PostgresTimeZone = 'UTC'
# vc_redist.x64 registers itself here; depending on the installer version under the 64-bit or the WOW6432Node view.
$VcRuntimeKeys = @(
    'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64')
# 1638: a newer version is already installed; 3010: installed, a restart is pending.
$VcRedistSuccessCodes = @(0, 1638, 3010)

# KLACKS_STATUS_FILE is set by the Setup.exe wrapper: it shows the last line on its progress page.
function Write-Status($message) {
    # Best effort: the wizard or a virus scanner may hold the file for a moment, and a lost line must never abort the install.
    if ($env:KLACKS_STATUS_FILE) { try { Add-Content -Path $env:KLACKS_STATUS_FILE -Value $message -Encoding Ascii } catch { } }
}

function Write-Step($message) {
    Write-Host "==> $message" -ForegroundColor Cyan
    Write-Status $message
}

function New-Secret([int]$bytes = 48) {
    $buffer = New-Object byte[] $bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($buffer)
    return [Convert]::ToBase64String($buffer)
}

function New-DatabasePassword {
    return (New-Secret 32).Replace('+', 'A').Replace('/', 'B').Replace('=', '')
}

function New-DataProtectionCertificate($password) {
    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    $request = New-Object System.Security.Cryptography.X509Certificates.CertificateRequest(
        'CN=Klacks DataProtection', $rsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddYears(10))
    $pfx = $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
    return [Convert]::ToBase64String($pfx)
}

function Set-DirectoryAccess($path, [string[]]$fullControlSids, [string[]]$readSids = @(), [string[]]$modifySids = @()) {
    # Replaces the inherited rights (C:\ProgramData grants every user read and create) with an explicit list.
    # Well-known SIDs, because account names are localised (e.g. VORDEFINIERT\Administratoren on German Windows).
    $acl = New-Object System.Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    $entries = @(
        @{ Sids = $fullControlSids; Rights = 'FullControl' },
        @{ Sids = $readSids; Rights = 'ReadAndExecute' },
        @{ Sids = $modifySids; Rights = 'Modify' })
    foreach ($entry in $entries) {
        foreach ($sid in $entry.Sids) {
            $identity = New-Object System.Security.Principal.SecurityIdentifier($sid)
            $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $identity, $entry.Rights, 'ContainerInherit,ObjectInherit', 'None', 'Allow')
            $acl.AddAccessRule($rule)
        }
    }
    Set-Acl -Path $path -AclObject $acl
}

function Protect-Directory($path) {
    # SYSTEM and Administrators only: Klacks and the updater run as LocalSystem; secrets, backups, documents and
    # import drop folders must not be readable or writable for other users of the PC.
    Set-DirectoryAccess $path @($LocalSystemSid, $AdministratorsSid)
}

function Invoke-Native($file, [string[]]$arguments) {
    & $file @arguments
    if ($LASTEXITCODE -ne 0) { throw "'$file $($arguments -join ' ')' exited with $LASTEXITCODE." }
}

function Get-ServiceSid([string]$serviceName) {
    # The per-service SID Windows derives from the name (S-1-5-80 plus the five little-endian DWORDs of the SHA-1 of
    # the upper-cased UTF-16 name); same computation as ServiceSid in the updater, so no account lookup is needed.
    $sha1 = [System.Security.Cryptography.SHA1]::Create()
    $hash = $sha1.ComputeHash([System.Text.Encoding]::Unicode.GetBytes($serviceName.ToUpperInvariant()))
    $subAuthorities = for ($i = 0; $i -lt 5; $i++) { [BitConverter]::ToUInt32($hash, $i * 4) }
    return 'S-1-5-80-' + ($subAuthorities -join '-')
}

# Brings KlacksStt to the registered state, whatever a previous run left behind. Mirrors the updater's
# WindowsSttServiceRegistrar: the service is created (Disabled) only when it is missing, every other step is
# idempotent, and the Environment value is written last as the "fully registered" marker. The start type of an
# existing service is left alone: the updater switches it to Automatic once the Parakeet engine is installed.
function Register-SttService($serviceName, $executable, $dataRoot) {
    $modelDirectory = Join-Path $dataRoot $SttModelDirectoryName
    $sttEnvironment = @(
        "ASPNETCORE_URLS=http://localhost:$SttPort", "Stt__ModelDirectory=$modelDirectory",
        "Stt__NumThreads=$SttThreads", "AllowedHosts=$AllowedHosts",
        'ASPNETCORE_ENVIRONMENT=Production', 'DOTNET_ENVIRONMENT=Production')
    New-Item -ItemType Directory -Force -Path $modelDirectory | Out-Null
    if (-not (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
        Write-Step "Registering service $serviceName (disabled)"
        New-Service -Name $serviceName -BinaryPathName "`"$executable`"" -DisplayName 'Klacks Speech Recognition' `
            -StartupType Disabled | Out-Null
    }
    else {
        Write-Step "Completing the registration of service $serviceName"
    }
    Invoke-Native 'sc.exe' @('sidtype', $serviceName, 'unrestricted') | Out-Null
    Invoke-Native 'sc.exe' @('config', $serviceName, 'obj=', "NT SERVICE\$serviceName") | Out-Null
    Invoke-Native 'sc.exe' @('failure', $serviceName, 'reset=', '86400',
        'actions=', 'restart/60000/restart/60000/restart/300000') | Out-Null
    Invoke-Native 'sc.exe' @('failureflag', $serviceName, '1') | Out-Null
    # Model downloads land here as the service account; nobody else on the PC may read or change the models.
    Set-DirectoryAccess $modelDirectory @($LocalSystemSid, $AdministratorsSid) @() @((Get-ServiceSid $serviceName))
    $key = "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName"
    New-ItemProperty -Path $key -Name 'Environment' -PropertyType MultiString -Value $sttEnvironment -Force | Out-Null
}

function Test-VcRuntime {
    foreach ($key in $VcRuntimeKeys) {
        $entry = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
        if ($entry -and $entry.Installed -eq 1) { return $true }
    }
    return $false
}

function Wait-Healthy($url, $timeoutSeconds, $process, $serviceName) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($process -and $process.HasExited) { throw "Klacks exited during start-up with code $($process.ExitCode)." }
        if ($serviceName -and (Get-Service -Name $serviceName).Status -eq 'Stopped') { throw "Service $serviceName stopped during start-up." }
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 10
            if ($response.StatusCode -eq 200) { return $true }
        }
        catch { }
        Start-Sleep -Seconds 5
    }
    return $false
}

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$') { throw "'$Version' is not a valid version." }
if (-not (Test-Path (Join-Path $PostgresDirectory 'bin\pg_ctl.exe'))) { throw "No PostgreSQL binaries below '$PostgresDirectory'." }

$versionsDirectory = Join-Path $ProgramRoot 'versions'
$versionDirectory = Join-Path $versionsDirectory $Version
$currentLink = Join-Path $ProgramRoot 'current'
$postgresRoot = Join-Path $ProgramRoot 'pgsql'
$postgresBin = Join-Path $postgresRoot 'bin'
$pgData = Join-Path $DataRoot 'pgdata'
$configDirectory = Join-Path $DataRoot 'config'
$logDirectory = Join-Path $DataRoot 'logs'
$backupDirectory = Join-Path $DataRoot 'backups'
$secretsFile = Join-Path $configDirectory $SecretsFileName
$configFile = Join-Path $configDirectory $ConfigFileName

# --- 1. Directories -----------------------------------------------------------------------------------------------
Write-Step "Preparing $ProgramRoot and $DataRoot"
foreach ($directory in @($ProgramRoot, $versionsDirectory, $DataRoot, $configDirectory, $logDirectory, $backupDirectory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}
if (-not $NoServices) {
    Protect-Directory $DataRoot
    Protect-Directory $configDirectory
    # Program files: only administrators may change them (the updater trusts an existing versions\<v> folder); the
    # PostgreSQL service account must still execute its binaries.
    Set-DirectoryAccess $ProgramRoot @($LocalSystemSid, $AdministratorsSid) @($AuthenticatedUsersSid)
}

if (-not $UpdatePublicKeyPath -and -not $AllowUnsignedUpdates) {
    throw 'Pass -UpdatePublicKeyPath (the vendor update signing key): without it updates and region packages are not verified. -AllowUnsignedUpdates is for test runs only.'
}
if (-not $UpdatePublicKeyPath) {
    Write-Warning 'Updates and region packages are NOT signature-verified on this installation (-AllowUnsignedUpdates).'
}

# --- 1b. Microsoft Visual C++ runtime (PostgreSQL and pgvector are MSVC builds) ----------------------------------
if (Test-VcRuntime) {
    Write-Step 'Microsoft Visual C++ runtime is installed'
}
elseif ($VcRedistPath) {
    Write-Step 'Installing the Microsoft Visual C++ runtime'
    $vcProcess = Start-Process -FilePath $VcRedistPath -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
    if ($VcRedistSuccessCodes -notcontains $vcProcess.ExitCode) { throw "vc_redist exited with $($vcProcess.ExitCode)." }
    if (-not (Test-VcRuntime)) { throw 'The Microsoft Visual C++ runtime is still missing after vc_redist.' }
}
else {
    Write-Warning 'The Microsoft Visual C++ runtime (x64) is not installed; PostgreSQL will not start without it.'
}

# --- 2. Secrets (generated once, kept on re-run) -------------------------------------------------------------------
if (Test-Path $secretsFile) {
    $secrets = Get-Content -Raw $secretsFile | ConvertFrom-Json
    Write-Step 'Reusing existing secrets'
}
else {
    Write-Step 'Generating secrets'
    $dataProtectionPassword = New-Secret 24
    $secrets = [pscustomobject]@{
        DatabasePassword             = New-DatabasePassword
        JwtSecret                    = New-Secret 64
        DataProtectionCertificate    = New-DataProtectionCertificate $dataProtectionPassword
        DataProtectionPassword       = $dataProtectionPassword
    }
    $secrets | ConvertTo-Json | Set-Content -Encoding UTF8 $secretsFile
}

# --- 3. PostgreSQL binaries and cluster ---------------------------------------------------------------------------
if (-not (Test-Path (Join-Path $postgresBin 'pg_ctl.exe'))) {
    Write-Step "Copying PostgreSQL binaries from $PostgresDirectory"
    New-Item -ItemType Directory -Force -Path $postgresRoot | Out-Null
    foreach ($part in @('bin', 'lib', 'share')) {
        Copy-Item -Recurse -Force (Join-Path $PostgresDirectory $part) (Join-Path $postgresRoot $part)
    }
}
if (-not (Test-Path (Join-Path $postgresRoot 'lib\vector.dll'))) { throw 'pgvector (lib\vector.dll) is missing from the PostgreSQL payload.' }

$postgresDataSids = @($LocalSystemSid, $AdministratorsSid, $NetworkServiceSid)
if (-not (Test-Path (Join-Path $pgData 'PG_VERSION'))) {
    Write-Step 'Initialising the database cluster'
    if (-not $NoServices) {
        # initdb drops the Administrators group from its token, so the installing account itself needs access while
        # it creates the cluster; that grant is removed again right after.
        New-Item -ItemType Directory -Force -Path $pgData | Out-Null
        $installingUserSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
        Set-DirectoryAccess $pgData ($postgresDataSids + $installingUserSid)
    }
    # initdb drops the Administrators group from its token, so the password file must not live in the protected config directory.
    $passwordFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -NoNewline -Encoding ASCII -Path $passwordFile -Value $secrets.DatabasePassword
        Invoke-Native (Join-Path $postgresBin 'initdb.exe') @('-D', $pgData, '-U', $PostgresSuperUser, '-E', 'UTF8',
            '--auth-local=scram-sha-256', '--auth-host=scram-sha-256', "--pwfile=$passwordFile", '--no-instructions')
    }
    finally {
        Remove-Item -Force -ErrorAction SilentlyContinue $passwordFile
    }
    Add-Content -Path (Join-Path $pgData 'postgresql.conf') -Value @(
        '', '# Klacks native installation', "port = $PostgresPort", "listen_addresses = 'localhost'",
        "timezone = '$PostgresTimeZone'", "log_timezone = '$PostgresTimeZone'")
}
if (-not $NoServices) { Set-DirectoryAccess $pgData $postgresDataSids }

$pgCtl = Join-Path $postgresBin 'pg_ctl.exe'
if ($NoServices) {
    & $pgCtl status -D $pgData *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Step 'Starting PostgreSQL (process mode)'
        Invoke-Native $pgCtl @('start', '-D', $pgData, '-w', '-l', (Join-Path $logDirectory 'postgres.log'))
    }
}
else {
    if (-not (Get-Service -Name $PostgresServiceName -ErrorAction SilentlyContinue)) {
        Write-Step "Registering service $PostgresServiceName"
        Invoke-Native $pgCtl @('register', '-N', $PostgresServiceName, '-D', $pgData, '-S', 'auto', '-w')
    }
    Start-Service -Name $PostgresServiceName
}

# --- 4. Program version and "current" junction -------------------------------------------------------------------
if (-not (Test-Path $versionDirectory)) {
    Write-Step "Installing Klacks $Version"
    $staging = Join-Path $ProgramRoot ('staging\' + [guid]::NewGuid().ToString('N'))
    Expand-Archive -Path $BundleZip -DestinationPath $staging
    Move-Item -Path $staging -Destination $versionDirectory
}
if (-not (Test-Path $currentLink)) {
    cmd /c mklink /J "$currentLink" "$versionDirectory" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not create the junction $currentLink." }
}

# --- 5. Configuration file (read by the API and the updater) -----------------------------------------------------
Write-Step "Writing $configFile"
$baseUrl = "http://localhost:$HttpPort"
$loopbackUrl = "http://127.0.0.1:$HttpPort"
$connectionString = "Host=localhost;Port=$PostgresPort;Database=$DatabaseName;Username=$PostgresSuperUser;Password=$($secrets.DatabasePassword)"
$publicKey = ''
if ($UpdatePublicKeyPath) { $publicKey = (Get-Content -Raw $UpdatePublicKeyPath).Replace("`r`n", "`n").Trim() }
$regionFile = ''
if ($Region) {
    $regionFile = Join-Path $currentLink ("app\deploy\onprem\regions\" + $Region.ToLowerInvariant() + '.json')
    if (-not (Test-Path $regionFile)) { throw "No region profile '$Region' in the bundle ($regionFile)." }
}

$config = [ordered]@{
    Urls              = $loopbackUrl
    AllowedHosts      = $AllowedHosts
    ConnectionStrings = [ordered]@{ DefaultConnection = $connectionString }
    JwtSettings       = [ordered]@{ Secret = $secrets.JwtSecret; ValidAudience = $baseUrl; ValidIssuer = $baseUrl }
    DataProtection    = [ordered]@{ CertificateBase64 = $secrets.DataProtectionCertificate; CertificatePassword = $secrets.DataProtectionPassword }
    Hosting           = [ordered]@{ UiRoot = (Join-Path $currentLink 'ui') }
    Database          = [ordered]@{ InitializeOnStartup = $true }
    Cors              = [ordered]@{ Host = $baseUrl; Home = $baseUrl }
    PasswordReset     = [ordered]@{ BaseUrl = $baseUrl }
    Mcp               = [ordered]@{ PublicBaseUrl = $baseUrl }
    # InitialAutoEnabled/InitialNotifyOnly: automatic updates are on for a new office-PC installation. Seeded into the
    # database only while no update setting is saved, so re-running this installer never overrides the admin UI.
    Update            = [ordered]@{
        ManifestBaseUrl    = $UpdateManifestBaseUrl
        SignaturePublicKey = $publicKey
        InitialAutoEnabled = 'true'
        InitialNotifyOnly  = 'false'
    }
    RegionSetup       = [ordered]@{ File = $regionFile }
    Updater           = [ordered]@{
        Mode                 = 'Native'
        NativeProgramRoot    = $ProgramRoot
        NativeServiceName    = $ApiServiceName
        ConnectionString     = $connectionString
        PostgresBinDirectory = $postgresBin
        BackupDirectory      = $backupDirectory
        HealthUrl            = "$loopbackUrl/internal/health/deep"
        VersionUrl           = "$loopbackUrl/api/Version"
        SignaturePublicKey   = $publicKey
        SttBaseUrl           = "http://localhost:$SttPort"
        NativeSttServiceName = $SttServiceName
    }
}
$config | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $configFile

# --- 6. Klacks and updater -------------------------------------------------------------------------------------
$apiExe = Join-Path $currentLink 'app\Klacks.Api.exe'
$updaterExe = Join-Path $currentLink 'updater\Klacks.Updater.exe'
$environment = @("Hosting__DataRoot=$DataRoot", 'ASPNETCORE_ENVIRONMENT=Production', 'DOTNET_ENVIRONMENT=Production')
$apiProcess = $null
$healthServiceName = $null

if ($NoServices) {
    Write-Step 'Starting Klacks (process mode)'
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo $apiExe
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = $env:SystemRoot
    foreach ($entry in $environment) {
        $pair = $entry.Split('=', 2)
        $startInfo.EnvironmentVariables[$pair[0]] = $pair[1]
    }
    $apiProcess = [System.Diagnostics.Process]::Start($startInfo)
}
else {
    # Register every service first and start them without waiting: Klacks reports "start pending" while it migrates
    # (up to 30 min on a first start), and Start-Service would block on it, so a later service was never registered.
    # Wait-Healthy below is the only place that waits.
    $services = @(@{ Name = $ApiServiceName; Exe = $apiExe; Display = 'Klacks'; DependsOn = @($PostgresServiceName) })
    if (Test-Path $updaterExe) {
        $services += @{ Name = $UpdaterServiceName; Exe = $updaterExe; Display = 'Klacks Updater'; DependsOn = @($PostgresServiceName) }
    }
    else {
        Write-Warning "The bundle contains no updater ($updaterExe); service $UpdaterServiceName is not registered."
    }
    foreach ($source in $EventLogSources) {
        # Best effort: the services log through a fail-safe provider and can still create the source themselves.
        try {
            if (-not [System.Diagnostics.EventLog]::SourceExists($source)) {
                Write-Step "Registering event log source $source"
                [System.Diagnostics.EventLog]::CreateEventSource($source, $EventLogName)
            }
        }
        catch {
            Write-Warning "Event log source $source could not be registered: $($_.Exception.Message)"
        }
    }
    foreach ($service in $services) {
        if (-not (Get-Service -Name $service.Name -ErrorAction SilentlyContinue)) {
            Write-Step "Registering service $($service.Name)"
            New-Service -Name $service.Name -BinaryPathName "`"$($service.Exe)`"" -DisplayName $service.Display `
                -StartupType Automatic -DependsOn $service.DependsOn | Out-Null
            $key = "HKLM:\SYSTEM\CurrentControlSet\Services\$($service.Name)"
            New-ItemProperty -Path $key -Name 'Environment' -PropertyType MultiString -Value $environment -Force | Out-Null
            sc.exe failure $service.Name reset= 86400 actions= restart/60000/restart/60000/restart/300000 | Out-Null
            # Also recover when the service reports itself stopped with an error code, not only after a crash.
            sc.exe failureflag $service.Name 1 | Out-Null
        }
    }
    # The speech recognition service is registered but never started here: Disabled until the user installs the
    # Parakeet engine, at which point the updater (LocalSystem) switches it to Automatic and starts it.
    $sttExe = Join-Path $currentLink 'stt\Klacks.Stt.exe'
    if (Test-Path $sttExe) {
        Register-SttService $SttServiceName $sttExe $DataRoot
    }
    else {
        Write-Warning "The bundle contains no speech recognition service ($sttExe); service $SttServiceName is not registered."
    }
    foreach ($service in $services) {
        $controller = Get-Service -Name $service.Name
        if ($controller.Status -eq 'Stopped') {
            Write-Step "Starting service $($service.Name)"
            $controller.Start()
        }
    }
    $healthServiceName = $ApiServiceName
}

# --- 7. Health -------------------------------------------------------------------------------------------------------
Write-Step "Waiting for Klacks on $loopbackUrl (first start migrates the database, this can take a few minutes)"
if (Wait-Healthy "$loopbackUrl/internal/health/deep" $HealthTimeoutSeconds $apiProcess $healthServiceName) {
    Write-Host "Klacks $Version is running: $baseUrl" -ForegroundColor Green
    Write-Status $ReadyMarker
}
else {
    Write-Warning "Klacks is still starting after $HealthTimeoutSeconds seconds (the first start downloads about 700 MB of AI models). Open $baseUrl later; logs: $logDirectory"
}
Write-Host "Default login:  admin@test.com  /  P@ssw0rt1  (Klacks then asks you to create your own admin account)" -ForegroundColor Green
