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

Write-Host "Version: $version"
Write-Host "Release Tag: $releaseTag"
Write-Host "Is Pre-Release: $isPreRelease"

# Set ADO pipeline variables
Write-Host "##vso[task.setvariable variable=PackageVersion]$version"
Write-Host "##vso[task.setvariable variable=ReleaseTag]$releaseTag"
Write-Host "##vso[task.setvariable variable=IsPreRelease]$isPreRelease"

# Update npm package versions
$npmRoot = Join-Path $env:BUILD_SOURCESDIRECTORY 'npm'
$packages = @(
    'safeguard-mcp',
    'safeguard-mcp-linux-x64',
    'safeguard-mcp-win-x64',
    'safeguard-mcp-darwin-arm64'
)

foreach ($pkg in $packages) {
    $pkgJsonPath = Join-Path $npmRoot $pkg 'package.json'
    if (Test-Path $pkgJsonPath) {
        $json = Get-Content $pkgJsonPath -Raw | ConvertFrom-Json
        $json.version = $version

        # Update optionalDependencies versions in root package
        if ($pkg -eq 'safeguard-mcp' -and $json.optionalDependencies) {
            $deps = $json.optionalDependencies
            foreach ($prop in @($deps.PSObject.Properties)) {
                $deps.($prop.Name) = $version
            }
        }

        $json | ConvertTo-Json -Depth 10 | Set-Content $pkgJsonPath -Encoding UTF8
        Write-Host "Updated $pkgJsonPath to version $version"
    }
}
