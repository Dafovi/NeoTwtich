[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release", "Test", "Verify", "Portable", "SelfContained", "Installer", "FullRelease")]
    [string]$Mode = "Debug",

    [string]$Runtime = "",

    [switch]$Clean
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build.common.ps1")

$BuildConfig = Get-NeoTwitchBuildConfig
$Runtime = if ([string]::IsNullOrWhiteSpace($Runtime)) { $BuildConfig.defaultRuntime } else { $Runtime }
$Solution = Resolve-NeoTwitchPath $BuildConfig.solution
$AppProject = Resolve-NeoTwitchPath $BuildConfig.appProject
$InstallerProject = Resolve-NeoTwitchPath $BuildConfig.installerProject
$TestProject = Resolve-NeoTwitchPath $BuildConfig.testProject
$DebugConfiguration = $BuildConfig.debugConfiguration
$ReleaseConfiguration = $BuildConfig.releaseConfiguration

function Invoke-DotNet {
    param([string[]]$Arguments)

    Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet fallo con codigo $LASTEXITCODE."
    }
}

function Resolve-BuiltAppExecutable {
    param([string]$Configuration)

    $projectDirectory = Split-Path -Parent $AppProject
    $configuredPath = Join-Path $projectDirectory "bin\$Configuration\net10.0-windows\$($BuildConfig.appExecutable)"
    if (Test-Path -LiteralPath $configuredPath) {
        return (Resolve-Path -LiteralPath $configuredPath).Path
    }

    $buildOutput = Join-Path $projectDirectory "bin\$Configuration"
    $candidate = Get-ChildItem -LiteralPath $buildOutput -Recurse -Filter $BuildConfig.appExecutable -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        throw "No encontre el ejecutable de $Configuration para la prueba de arranque."
    }

    return $candidate.FullName
}

function Invoke-AppSmokeTest {
    param([string]$Configuration)

    $processName = [System.IO.Path]::GetFileNameWithoutExtension($BuildConfig.appExecutable)
    $existingProcess = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($existingProcess) {
        throw "No se puede verificar el arranque mientras Neo Twitch ya esta abierto. Cierra la app y vuelve a ejecutar la verificacion."
    }

    $exe = Resolve-BuiltAppExecutable $Configuration
    $smokeProfile = Join-Path $ArtifactRoot "smoke-profile"
    New-Item -ItemType Directory -Path $smokeProfile -Force | Out-Null
    $crashLog = Join-Path $smokeProfile "NeoTwitch\crash.log"

    Write-Host "Smoke test: $exe" -ForegroundColor Cyan
    $previousAppData = $env:APPDATA
    $previousLocalAppData = $env:LOCALAPPDATA
    try {
        $env:APPDATA = $smokeProfile
        $env:LOCALAPPDATA = $smokeProfile
        $process = Start-Process -FilePath $exe -ArgumentList @("--debug", "--safe-mode", "--no-autoconnect", "--no-start-hidden") -PassThru
    }
    finally {
        $env:APPDATA = $previousAppData
        $env:LOCALAPPDATA = $previousLocalAppData
    }

    try {
        Start-Sleep -Seconds 5

        $crashChanged = $false
        if (Test-Path -LiteralPath $crashLog) {
            $crashChanged = $true
        }

        if ($crashChanged) {
            throw "La app escribio un nuevo crash.log durante el arranque: $crashLog"
        }

        if ($process.HasExited) {
            throw "La app se cerro durante el smoke test con codigo $($process.ExitCode)."
        }
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            $null = $process.CloseMainWindow()
            Start-Sleep -Seconds 1
            if (-not $process.HasExited) {
                $process.Kill()
                $process.WaitForExit()
            }
        }
    }

    Write-Host "Smoke test listo: la app inicio sin crash de arranque." -ForegroundColor Green
}

function Invoke-TestSuite {
    param(
        [string]$Configuration,
        [switch]$NoBuild,
        [switch]$NoRestore
    )

    $parameters = @{ Configuration = $Configuration }
    if ($NoBuild) { $parameters.NoBuild = $true }
    if ($NoRestore) { $parameters.NoRestore = $true }
    & (Join-Path $PSScriptRoot "test.ps1") @parameters
}

function Invoke-Verification {
    Invoke-DotNet @("restore", $Solution, "--locked-mode")
    Invoke-DotNet @("build", $Solution, "-c", $ReleaseConfiguration, "--no-restore")
    Invoke-TestSuite -Configuration $ReleaseConfiguration -NoBuild -NoRestore
    Invoke-AppSmokeTest $ReleaseConfiguration
}

function Publish-Portable {
    param([string]$ArtifactRoot)

    $output = Join-Path $ArtifactRoot (Expand-NeoTwitchPattern $BuildConfig.portableDirectoryPattern $Version)
    $zip = Join-Path $ArtifactRoot (Expand-NeoTwitchPattern $BuildConfig.portableZipPattern $Version)

    Invoke-DotNet @(
        "publish", $AppProject,
        "-c", $ReleaseConfiguration,
        "-r", $Runtime,
        "--self-contained", "false",
        "-p:PublishSingleFile=false",
        "-o", $output
    )

    if (Test-Path -LiteralPath $zip) {
        Remove-Item -LiteralPath $zip -Force
    }

    Compress-Archive -Path (Join-Path $output "*") -DestinationPath $zip -Force
}

function Publish-SelfContained {
    param([string]$ArtifactRoot)

    $output = Join-Path $ArtifactRoot $BuildConfig.selfContainedWorkDirectory
    Invoke-DotNet @(
        "publish", $AppProject,
        "-c", $ReleaseConfiguration,
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o", $output
    )

    Copy-Item -LiteralPath (Join-Path $output $BuildConfig.appExecutable) -Destination (Join-Path $ArtifactRoot $BuildConfig.appExecutable) -Force
}

function Publish-Installer {
    param([string]$ArtifactRoot)

    & (Join-Path $PSScriptRoot "validate-release-trust.ps1")
    $output = Join-Path $ArtifactRoot $BuildConfig.installerWorkDirectory
    Invoke-DotNet @(
        "publish", $InstallerProject,
        "-c", $ReleaseConfiguration,
        "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true",
        "-p:RequireProductionReleaseTrustRoot=true",
        "-o", $output
    )

    $installerAssembly = Get-ChildItem -LiteralPath (Split-Path -Parent $InstallerProject) -Recurse -Filter "NeoTwitch.Installer.dll" |
        Where-Object { $_.FullName -notmatch "\\(ref|refint)\\" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $installerAssembly) { throw "No se encontro el ensamblado publicado del instalador." }
    & (Join-Path $PSScriptRoot "validate-release-trust.ps1") -InstallerAssemblyPath $installerAssembly.FullName

    Copy-Item -LiteralPath (Join-Path $output $BuildConfig.installerExecutable) -Destination (Join-Path $ArtifactRoot $BuildConfig.installerExecutable) -Force
}

$Version = Get-NeoTwitchVersion $BuildConfig
$ArtifactRoot = Get-NeoTwitchArtifactRoot $BuildConfig $Version

if ($Mode -in @("Installer", "FullRelease")) {
    & (Join-Path $PSScriptRoot "validate-release-trust.ps1")
}

if ($Clean -and (Test-Path -LiteralPath $ArtifactRoot)) {
    Remove-Item -LiteralPath $ArtifactRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $ArtifactRoot -Force | Out-Null

switch ($Mode) {
    "Debug" {
        Invoke-DotNet @("build", $AppProject, "-c", $DebugConfiguration)
    }
    "Release" {
        Invoke-DotNet @("build", $AppProject, "-c", $ReleaseConfiguration)
        Invoke-DotNet @("build", $InstallerProject, "-c", $ReleaseConfiguration)
    }
    "Test" {
        Invoke-TestSuite -Configuration $DebugConfiguration
    }
    "Verify" {
        Invoke-Verification
    }
    "Portable" {
        Publish-Portable $ArtifactRoot
    }
    "SelfContained" {
        Publish-SelfContained $ArtifactRoot
    }
    "Installer" {
        Publish-Installer $ArtifactRoot
    }
    "FullRelease" {
        Invoke-Verification
        Publish-Portable $ArtifactRoot
        Publish-SelfContained $ArtifactRoot
        Publish-Installer $ArtifactRoot
    }
}

Write-Host "Build listo: $Mode -> $ArtifactRoot" -ForegroundColor Green
