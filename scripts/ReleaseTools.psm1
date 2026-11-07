Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:VnaRids = @('win-x86', 'win-x64', 'win-arm64', 'linux-x64', 'linux-arm', 'linux-arm64',
    'linux-musl-x64', 'linux-musl-arm', 'linux-musl-arm64', 'osx-x64', 'osx-arm64')

function Get-VnaSupportedRids {
    return @($script:VnaRids)
}

function Get-VnaReleaseVersion {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Tag)

    if ($Tag -cnotmatch '\Av(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z') {
        throw "Release tag must have the exact format vX.Y.Z, without leading zeroes: '$Tag'."
    }
    for ($index = 1; $index -le 3; $index++) {
        $component = 0
        if (-not [int]::TryParse($Matches[$index], [ref]$component) -or $component -gt 65534) {
            throw 'Version components must be 0–65534 for .NET assembly versioning.'
        }
    }
    return $Tag.Substring(1)
}

function Get-VnaPackageName {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Tag, [Parameter(Mandatory)][string]$Rid)

    if ($Rid -cnotin $script:VnaRids) { throw "Unsupported RID: '$Rid'." }
    $version = Get-VnaReleaseVersion -Tag $Tag
    return "VnaStudio-$version-$Rid"
}

function Assert-VnaPublishOutput {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Directory, [Parameter(Mandatory)][string]$Tag, [Parameter(Mandatory)][string]$Rid)

    $null = Get-VnaPackageName -Tag $Tag -Rid $Rid
    $executable = if ($Rid.StartsWith('win-')) { 'Vna.App.exe' } else { 'Vna.App' }
    $runtime = if ($Rid.StartsWith('win-')) { 'coreclr.dll' } elseif ($Rid.StartsWith('osx-')) { 'libcoreclr.dylib' } else { 'libcoreclr.so' }
    $required = @($executable, $runtime, 'Vna.App.dll', 'Vna.App.deps.json', 'Vna.App.runtimeconfig.json',
        'Vna.Core.dll', 'Vna.Devices.dll', 'Vna.Device.Vna65MHz.dll', 'Vna.IO.dll', 'Vna.Localization.dll',
        'pl/Vna.Localization.resources.dll', 'Assets/analyser306b-65mhz.ini', 'release-info.json')
    if ($Rid.StartsWith('win-')) {
        $required += @('libSkiaSharp.dll', 'libHarfBuzzSharp.dll')
    } elseif ($Rid.StartsWith('osx-')) {
        $required += @('libSkiaSharp.dylib', 'libHarfBuzzSharp.dylib', 'libAvaloniaNative.dylib', 'libSystem.IO.Ports.Native.dylib')
    } else {
        $required += @('libSkiaSharp.so', 'libHarfBuzzSharp.so', 'libSystem.IO.Ports.Native.so')
    }
    foreach ($relativePath in $required) {
        $path = Join-Path $Directory $relativePath
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) {
            throw "Missing or empty published file: $relativePath"
        }
    }
    if ($Rid.StartsWith('win-')) {
        $expectedMachine = switch ($Rid) { 'win-x86' { 0x014c } 'win-x64' { 0x8664 } 'win-arm64' { 0xaa64 } }
        foreach ($file in @($executable, $runtime, 'libSkiaSharp.dll', 'libHarfBuzzSharp.dll')) {
            $stream = [IO.File]::OpenRead((Join-Path $Directory $file))
            try {
                $reader = [Reflection.PortableExecutable.PEReader]::new($stream)
                try {
                    if ([int]$reader.PEHeaders.CoffHeader.Machine -ne $expectedMachine) {
                        throw "Native file '$file' has the wrong architecture for '$Rid'."
                    }
                } finally { $reader.Dispose() }
            } finally { $stream.Dispose() }
        }
    }
    $deps = Get-Content -LiteralPath (Join-Path $Directory 'Vna.App.deps.json') -Raw | ConvertFrom-Json
    if (-not $deps.runtimeTarget.name.EndsWith("/$Rid", [StringComparison]::Ordinal)) {
        throw "Published dependencies do not match RID '$Rid'."
    }
    $runtimeConfig = Get-Content -LiteralPath (Join-Path $Directory 'Vna.App.runtimeconfig.json') -Raw | ConvertFrom-Json
    if (-not $runtimeConfig.runtimeOptions.PSObject.Properties['includedFrameworks'] -or
        @($runtimeConfig.runtimeOptions.includedFrameworks).Count -eq 0) {
        throw 'The package must contain a self-contained .NET runtime.'
    }
    $version = Get-VnaReleaseVersion -Tag $Tag
    $metadata = Get-Content -LiteralPath (Join-Path $Directory 'release-info.json') -Raw | ConvertFrom-Json
    if ($metadata.version -cne $version -or $metadata.tag -cne $Tag -or $metadata.rid -cne $Rid -or $metadata.selfContained -ne $true) {
        throw 'Release metadata does not match the requested tag and RID.'
    }
    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $Directory 'Vna.App.dll')).Version.ToString()
    if ($assemblyVersion -cne "$version.0") { throw "Assembly version '$assemblyVersion' does not match tag '$Tag'." }
}

function New-VnaReleaseArchive {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Directory, [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$Tag, [Parameter(Mandatory)][string]$Rid)

    Assert-VnaPublishOutput -Directory $Directory -Tag $Tag -Rid $Rid
    $name = Get-VnaPackageName -Tag $Tag -Rid $Rid
    $directoryInfo = Get-Item -LiteralPath $Directory
    if ($directoryInfo.Name -cne $name) { throw "The publish directory must be named '$name'." }
    if (-not $Rid.StartsWith('win-') -and $IsWindows) {
        throw 'Create Unix archives on Linux or macOS to preserve executable permissions.'
    }
    $extension = if ($Rid.StartsWith('win-')) { '.zip' } else { '.tar.gz' }
    $null = New-Item -ItemType Directory -Path $Destination -Force
    $archive = Join-Path ([IO.Path]::GetFullPath($Destination)) "$name$extension"
    if ((Test-Path -LiteralPath $archive) -or (Test-Path -LiteralPath "$archive.sha256")) {
        throw "Release output already exists: $archive"
    }
    if ($Rid.StartsWith('win-')) {
        [IO.Compression.ZipFile]::CreateFromDirectory($directoryInfo.FullName, $archive, [IO.Compression.CompressionLevel]::Optimal, $true)
    } else {
        & chmod +x (Join-Path $directoryInfo.FullName 'Vna.App')
        if ($LASTEXITCODE -ne 0) { throw 'Could not set executable permissions.' }
        & tar -czf $archive -C $directoryInfo.Parent.FullName $name
        if ($LASTEXITCODE -ne 0) { throw 'Could not create the release archive.' }
    }
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$archive.sha256", "$hash  $([IO.Path]::GetFileName($archive))`n", [Text.UTF8Encoding]::new($false))
    return $archive
}

function Get-VnaReleaseAssets {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Tag, [Parameter(Mandatory)][string]$Directory)

    $directoryPath = (Resolve-Path -LiteralPath $Directory).Path
    $archives = @()
    $checksums = @()
    foreach ($rid in $script:VnaRids) {
        $name = Get-VnaPackageName -Tag $Tag -Rid $rid
        $extension = if ($rid.StartsWith('win-')) { '.zip' } else { '.tar.gz' }
        $archive = Join-Path $directoryPath "$name$extension"
        if (-not (Test-Path -LiteralPath $archive -PathType Leaf) -or -not (Test-Path -LiteralPath "$archive.sha256" -PathType Leaf)) {
            throw "Missing archive or checksum for RID '$rid'."
        }
        $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
        $expected = "$hash  $name$extension"
        $recorded = (Get-Content -LiteralPath "$archive.sha256" -Raw).TrimEnd("`r", "`n")
        if ($recorded -cne $expected) { throw "Checksum mismatch for RID '$rid'." }
        $archives += $archive
        $checksums += $expected
    }
    $allowedNames = @($archives | ForEach-Object { [IO.Path]::GetFileName($_); [IO.Path]::GetFileName($_) + '.sha256' }) + @('SHA256SUMS.txt')
    foreach ($entry in Get-ChildItem -LiteralPath $directoryPath -Force) {
        if ($entry.PSIsContainer -or $entry.Name -cnotin $allowedNames) { throw "Unexpected release artifact: $($entry.Name)" }
    }
    $manifest = Join-Path $directoryPath 'SHA256SUMS.txt'
    [IO.File]::WriteAllText($manifest, (($checksums | Sort-Object) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    return @($archives) + @($manifest)
}

Export-ModuleMember -Function Get-VnaSupportedRids, Get-VnaReleaseVersion, Get-VnaPackageName, Assert-VnaPublishOutput, New-VnaReleaseArchive, Get-VnaReleaseAssets
