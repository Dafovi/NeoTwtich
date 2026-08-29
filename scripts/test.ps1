[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$NoBuild,

    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build.common.ps1")

$buildConfig = Get-NeoTwitchBuildConfig
$testProject = Resolve-NeoTwitchPath $buildConfig.testProject
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resultsDirectory = Join-Path $repositoryRoot "artifacts\test-results\$Configuration"
$resultFileName = "NeoTwitch-$Configuration.trx"
$resultPath = Join-Path $resultsDirectory $resultFileName

New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
if (Test-Path -LiteralPath $resultPath) {
    Remove-Item -LiteralPath $resultPath -Force
}

$arguments = @(
    "test", $testProject,
    "-c", $Configuration,
    "--logger", "trx;LogFileName=$resultFileName",
    "--results-directory", $resultsDirectory
)

if ($NoBuild) {
    $arguments += "--no-build"
}

if ($NoRestore) {
    $arguments += "--no-restore"
}

Write-Host "dotnet $($arguments -join ' ')" -ForegroundColor Cyan
& dotnet @arguments
$testExitCode = $LASTEXITCODE

if (-not (Test-Path -LiteralPath $resultPath)) {
    throw "dotnet test no produjo el resultado TRX esperado: $resultPath"
}

[xml]$trx = Get-Content -LiteralPath $resultPath -Raw
$counters = $trx.SelectSingleNode("/*[local-name()='TestRun']/*[local-name()='ResultSummary']/*[local-name()='Counters']")
if ($null -eq $counters) {
    throw "El resultado TRX no contiene contadores verificables: $resultPath"
}

$total = [int]$counters.total
$executed = [int]$counters.executed
$passed = [int]$counters.passed
$failed = [int]$counters.failed
$minimum = [int]$buildConfig.minimumDiscoveredTests

Write-Host "Tests descubiertos: $total; ejecutados: $executed; aprobados: $passed; fallidos: $failed; minimo: $minimum." -ForegroundColor Cyan

if ($total -lt $minimum) {
    throw "La verificacion rechazo la suite: se descubrieron $total tests y el minimo es $minimum. Un resultado sin tests nunca es valido."
}

if ($executed -ne $total) {
    throw "La verificacion rechazo la suite: se ejecutaron $executed de $total tests descubiertos."
}

if ($testExitCode -ne 0 -or $failed -gt 0 -or $passed -ne $total) {
    throw "La suite fallo (codigo $testExitCode; aprobados $passed; fallidos $failed; total $total)."
}

Write-Host "Suite verificada con descubrimiento real: $total tests aprobados." -ForegroundColor Green
