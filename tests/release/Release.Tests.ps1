Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repository = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Import-Module (Join-Path $repository 'scripts/ReleaseTools.psm1') -Force
$script:checks = 0

function Assert-Equal($Expected, $Actual, [string]$Description) {
    if ($Expected -cne $Actual) { throw "$Description : expected '$Expected', got '$Actual'." }
    $script:checks++
}

function Assert-Throws([scriptblock]$Action, [string]$Description) {
    $threw = $false
    try { & $Action | Out-Null } catch { $threw = $true }
    if (-not $threw) { throw "$Description : expected an error." }
    $script:checks++
}

foreach ($tag in @('v0.0.0', 'v1.2.3', 'v10.20.30', 'v65534.0.1')) {
    Assert-Equal $tag.Substring(1) (Get-VnaReleaseVersion -Tag $tag) "Accepted tag $tag"
}
foreach ($tag in @('1.2.3', 'V1.2.3', 'v1.2', 'v01.2.3', 'v1.02.3', 'v1.2.03', 'v1.2.3-beta',
    'v1.2.3+build', 'v65535.0.0', 'v99999999999999999999.0.0', "v1.2.3`n", 'v1.2.3;echo injected')) {
    Assert-Throws { Get-VnaReleaseVersion -Tag $tag } "Rejected tag $tag"
}
$rids = @(Get-VnaSupportedRids)
Assert-Equal 11 $rids.Count 'All supported desktop RIDs'
$workflow = Get-Content -LiteralPath (Join-Path $repository '.github/workflows/release.yml') -Raw
$workflowRids = @([regex]::Matches($workflow, '(?m)^\s+- rid:\s*(\S+)\s*$') | ForEach-Object { $_.Groups[1].Value })
Assert-Equal (($rids | Sort-Object) -join ',') (($workflowRids | Sort-Object) -join ',') 'Workflow and packaging have the same RID matrix'
$publishCommand = Get-Command (Join-Path $repository 'scripts/Publish-Release.ps1')
$cliRids = @($publishCommand.Parameters['Rid'].Attributes | Where-Object { $_ -is [Management.Automation.ValidateSetAttribute] } | ForEach-Object { $_.ValidValues })
Assert-Equal (($rids | Sort-Object) -join ',') (($cliRids | Sort-Object) -join ',') 'Publish CLI accepts the complete RID matrix'
foreach ($rid in $rids) { Assert-Equal "VnaStudio-1.2.3-$rid" (Get-VnaPackageName -Tag v1.2.3 -Rid $rid) "Package name $rid" }
Assert-Throws { Get-VnaPackageName -Tag v1.2.3 -Rid win-arm } 'Unsupported RID'
Assert-Throws { Get-VnaPackageName -Tag v1.2.3 -Rid WIN-X64 } 'RID case'

$root = [IO.Path]::GetFullPath((Join-Path $repository ".artifacts/release-tests/$([Guid]::NewGuid().ToString('N'))"))
$null = New-Item -ItemType Directory -Path $root
try {
    Assert-Throws { Get-VnaReleaseAssets -Tag v1.2.3 -Directory $root } 'Incomplete release'
    Assert-Throws { Assert-VnaPublishOutput -Directory $root -Tag v1.2.3 -Rid win-x64 } 'Missing publish files'
    foreach ($rid in $rids) {
        $extension = if ($rid.StartsWith('win-')) { '.zip' } else { '.tar.gz' }
        $name = "VnaStudio-1.2.3-$rid$extension"
        $archive = Join-Path $root $name
        [IO.File]::WriteAllText($archive, "fixture-$rid")
        $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
        [IO.File]::WriteAllText("$archive.sha256", "$hash  $name`n")
    }
    $assets = @(Get-VnaReleaseAssets -Tag v1.2.3 -Directory $root)
    Assert-Equal ($rids.Count + 1) $assets.Count 'All archives and one combined checksum manifest'
    $manifest = Get-Content -LiteralPath (Join-Path $root 'SHA256SUMS.txt')
    Assert-Equal $rids.Count $manifest.Count 'Manifest has all supported RIDs'
    Assert-Equal ($rids.Count + 1) @(Get-VnaReleaseAssets -Tag v1.2.3 -Directory $root).Count 'Manifest generation can be repeated'

    $changed = Join-Path $root 'VnaStudio-1.2.3-win-x64.zip'
    [IO.File]::WriteAllText($changed, 'corrupt')
    Assert-Throws { Get-VnaReleaseAssets -Tag v1.2.3 -Directory $root } 'Corrupt archive'
    [IO.File]::WriteAllText($changed, 'fixture-win-x64')

    $checksum = "$changed.sha256"
    $originalChecksum = [IO.File]::ReadAllText($checksum)
    [IO.File]::WriteAllText($checksum, 'invalid')
    Assert-Throws { Get-VnaReleaseAssets -Tag v1.2.3 -Directory $root } 'Corrupt checksum'
    [IO.File]::WriteAllText($checksum, $originalChecksum)

    $unexpected = Join-Path $root 'unexpected.zip'
    [IO.File]::WriteAllText($unexpected, 'unexpected')
    Assert-Throws { Get-VnaReleaseAssets -Tag v1.2.3 -Directory $root } 'Unexpected asset'
    Remove-Item -LiteralPath $unexpected
    Assert-Throws { Get-VnaReleaseAssets -Tag v2.0.0 -Directory $root } 'Assets from another release'
} finally {
    $expectedRoot = [IO.Path]::GetFullPath((Join-Path $repository '.artifacts/release-tests')) + [IO.Path]::DirectorySeparatorChar
    if (-not $root.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $root -Recurse -Force
}
Write-Output "Release checks passed: $script:checks"
