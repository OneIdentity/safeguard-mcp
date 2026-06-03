param(
    [Parameter(Mandatory=$true)]
    [int]$BuildId,
    [Parameter(Mandatory=$true)]
    [string]$TagName,
    [Parameter(Mandatory=$true)]
    [string]$IsTagBuild,
    [Parameter(Mandatory=$true)]
    [string]$SemanticVersion
)

$ErrorActionPreference = 'Stop'

if ($IsTagBuild -eq 'true') {
    # Tag build: use the tag as the version (strip leading 'v')
    $version = $TagName -replace '^v', ''
    $releaseTag = "v$version"
    $isPreRelease = $false
}
else {
    # Non-tag build: append pre-release suffix
    $preSuffix = $BuildId % 65534
    $version = "$SemanticVersion-pre$preSuffix"
    $releaseTag = "dev/v$version"
    $isPreRelease = $true
}

# Validate the computed version is a recognizable SemVer (loose form: x.y.z
# with optional pre-release / build metadata). Catches bad tag names early.
if ($version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$') {
    Write-Error "Computed version '$version' is not valid SemVer."
    exit 1
}

Write-Host "Version: $version"
Write-Host "Release Tag: $releaseTag"
Write-Host "Is Pre-Release: $isPreRelease"

# Set ADO pipeline variables
Write-Host "##vso[task.setvariable variable=PackageVersion]$version"
Write-Host "##vso[task.setvariable variable=ReleaseTag]$releaseTag"
Write-Host "##vso[task.setvariable variable=IsPreRelease]$isPreRelease"

$repoRoot = if ($env:BUILD_SOURCESDIRECTORY) { $env:BUILD_SOURCESDIRECTORY } else { (Get-Location).Path }

# ---------------------------------------------------------------------------
# Stamp csproj <Version> (XML-aware to avoid regex pitfalls).
#
# NOTE: AOT publish jobs may run before this script in the pipeline. If you
# need the binary's embedded version to match the stamped value, either move
# the version-stamp step ahead of the publish jobs or pass
# `-p:Version=$(PackageVersion)` to dotnet publish.
# ---------------------------------------------------------------------------
$csprojPath = Join-Path $repoRoot 'src/SafeguardMcp/SafeguardMcp.csproj'
[xml]$csprojXml = Get-Content -LiteralPath $csprojPath
$versionNodes = $csprojXml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ }
if ($null -eq $versionNodes -or @($versionNodes).Count -eq 0) {
    Write-Error "$csprojPath does not contain a <Version> element to stamp."
    exit 1
}
if (@($versionNodes).Count -gt 1) {
    Write-Error "$csprojPath contains multiple <Version> elements; refusing to stamp ambiguously."
    exit 1
}
foreach ($pg in $csprojXml.Project.PropertyGroup) {
    if ($pg.Version) { $pg.Version = $version }
}
$csprojXml.Save($csprojPath)
Write-Host "Updated $csprojPath to version $version"

# ---------------------------------------------------------------------------
# Stamp npm packages. The platform package list is derived from the root
# package's optionalDependencies, so adding/removing a platform package only
# requires editing the root manifest -- this script (and the drift guard
# below) auto-expand to match.
# ---------------------------------------------------------------------------
$npmRoot = Join-Path $repoRoot 'npm'
$rootPkgPath = Join-Path $npmRoot 'safeguard-mcp/package.json'
if (-not (Test-Path -LiteralPath $rootPkgPath)) {
    Write-Error "Missing $rootPkgPath"
    exit 1
}

$rootJson = Get-Content -LiteralPath $rootPkgPath -Raw | ConvertFrom-Json
$rootJson.version = $version

$platformPackages = @()
if ($rootJson.optionalDependencies) {
    foreach ($prop in @($rootJson.optionalDependencies.PSObject.Properties)) {
        $rootJson.optionalDependencies.($prop.Name) = $version
        # Convert "@oneidentity/safeguard-mcp-linux-x64" -> "safeguard-mcp-linux-x64"
        $bareName = ($prop.Name -split '/', 2)[-1]
        $platformPackages += $bareName
    }
}
$rootJson | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $rootPkgPath -Encoding UTF8
Write-Host "Updated $rootPkgPath to version $version"

foreach ($pkg in $platformPackages) {
    $pkgJsonPath = Join-Path $npmRoot $pkg 'package.json'
    if (-not (Test-Path -LiteralPath $pkgJsonPath)) {
        Write-Error "Missing $pkgJsonPath (referenced from root optionalDependencies)."
        exit 1
    }
    $json = Get-Content -LiteralPath $pkgJsonPath -Raw | ConvertFrom-Json
    $json.version = $version
    $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $pkgJsonPath -Encoding UTF8
    Write-Host "Updated $pkgJsonPath to version $version"
}

# ---------------------------------------------------------------------------
# Stamp server.json (top-level + each packages[].version).
# ---------------------------------------------------------------------------
$serverJsonPath = Join-Path $repoRoot 'server.json'
$serverJson = Get-Content -LiteralPath $serverJsonPath -Raw | ConvertFrom-Json
$serverJson.version = $version
if ($serverJson.packages) {
    foreach ($p in $serverJson.packages) {
        $p.version = $version
    }
}
$serverJson | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $serverJsonPath -Encoding UTF8
Write-Host "Updated $serverJsonPath to version $version"

# ---------------------------------------------------------------------------
# Drift guard. Re-read every stamped manifest and confirm every version
# field equals $version. Fail loudly with a list of every mismatch.
# ---------------------------------------------------------------------------
$mismatches = @()

[xml]$csprojCheck = Get-Content -LiteralPath $csprojPath
$csprojVer = $null
foreach ($pg in $csprojCheck.Project.PropertyGroup) {
    if ($pg.Version) { $csprojVer = $pg.Version }
}
if ($csprojVer -ne $version) {
    $mismatches += "$csprojPath <Version>: '$csprojVer' != '$version'"
}

$rootCheck = Get-Content -LiteralPath $rootPkgPath -Raw | ConvertFrom-Json
if ($rootCheck.version -ne $version) {
    $mismatches += "$rootPkgPath .version: '$($rootCheck.version)' != '$version'"
}
if ($rootCheck.optionalDependencies) {
    foreach ($prop in $rootCheck.optionalDependencies.PSObject.Properties) {
        if ($prop.Value -ne $version) {
            $mismatches += "$rootPkgPath optionalDependencies['$($prop.Name)']: '$($prop.Value)' != '$version'"
        }
    }
}

foreach ($pkg in $platformPackages) {
    $pkgJsonPath = Join-Path $npmRoot $pkg 'package.json'
    $check = Get-Content -LiteralPath $pkgJsonPath -Raw | ConvertFrom-Json
    if ($check.version -ne $version) {
        $mismatches += "$pkgJsonPath .version: '$($check.version)' != '$version'"
    }
}

$serverCheck = Get-Content -LiteralPath $serverJsonPath -Raw | ConvertFrom-Json
if ($serverCheck.version -ne $version) {
    $mismatches += "$serverJsonPath .version: '$($serverCheck.version)' != '$version'"
}
if ($serverCheck.packages) {
    for ($i = 0; $i -lt $serverCheck.packages.Count; $i++) {
        $pv = $serverCheck.packages[$i].version
        if ($pv -ne $version) {
            $mismatches += "$serverJsonPath packages[$i].version: '$pv' != '$version'"
        }
    }
}

if ($mismatches.Count -gt 0) {
    Write-Error ("Version drift detected after stamping:`n  - " + ($mismatches -join "`n  - "))
    exit 1
}

Write-Host "Version drift guard passed: all manifests at $version"
