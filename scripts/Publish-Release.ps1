[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][ValidateSet('win-x86', 'win-x64', 'win-arm64', 'linux-x64', 'linux-arm', 'linux-arm64', 'linux-musl-x64', 'linux-musl-arm', 'linux-musl-arm64', 'osx-x64', 'osx-arm64')][string]$Rid,
    [string]$ArtifactsDirectory,
    [string]$RestoreSource,
    [switch]$PublishOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReleaseTools.psm1') -Force
$repository = Split-Path $PSScriptRoot -Parent
if (-not $ArtifactsDirectory) { $ArtifactsDirectory = Join-Path $repository '.artifacts/release' }
$ArtifactsDirectory = [IO.Path]::GetFullPath($ArtifactsDirectory)
$version = Get-VnaReleaseVersion -Tag $Tag
$name = Get-VnaPackageName -Tag $Tag -Rid $Rid
$publish = Join-Path $ArtifactsDirectory "publish/$Rid/$name"
if (Test-Path -LiteralPath $publish) { throw "Publish directory already exists: $publish" }

$arguments = @('publish', (Join-Path $repository 'src/Vna.App/Vna.App.csproj'),
    '--configuration', 'Release', '--runtime', $Rid, '--self-contained', 'true', '--output', $publish,
    '-p:PublishSingleFile=false', '-p:PublishTrimmed=false', '-p:UseAppHost=true',
    "-p:Version=$version", "-p:AssemblyVersion=$version.0", "-p:FileVersion=$version.0",
    "-p:InformationalVersion=$version", '-p:ContinuousIntegrationBuild=true',
    '-p:UseArtifactsOutput=true', "-p:ArtifactsPath=$(Join-Path $ArtifactsDirectory "build/$Rid")", '-p:UsedAvaloniaProducts=')
if ($RestoreSource) { $arguments += @("-p:RestoreSources=$RestoreSource", '-p:NuGetAudit=false') }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for '$Rid'." }

$metadata = [ordered]@{ application = 'VnaStudio'; version = $version; tag = $Tag; rid = $Rid; selfContained = $true }
$metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $publish 'release-info.json') -Encoding utf8NoBOM
# Validate every release publish before either packaging it or returning it for inspection.
Assert-VnaPublishOutput -Directory $publish -Tag $Tag -Rid $Rid
if ($PublishOnly) {
    return $publish
}
$archive = New-VnaReleaseArchive -Directory $publish -Destination (Join-Path $ArtifactsDirectory 'packages') -Tag $Tag -Rid $Rid
Write-Output $archive
