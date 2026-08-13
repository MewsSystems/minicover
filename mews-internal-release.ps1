<#
.SYNOPSIS
    Packs the Mews MiniCover fork and publishes it to the internal Azure Artifacts feed.

.DESCRIPTION
    The version is derived, never hand-written:

        <VersionPrefix>-rel.<yyyyMMddHHmm>            when publishing from the release branch
        <VersionPrefix>-dev.<TICKET>.<yyyyMMddHHmm>   when publishing from any other branch

    VersionPrefix comes from src/Directory.Build.props and tracks the upstream version the
    fork is based on. The timestamp is UTC, so versions order without anyone having to look
    up what was published last.

    NuGet compares pre-release labels case-insensitively, one dot-separated identifier at a
    time. `dev` < `rel`, and it is the first identifier, so a branch build can never outrank
    a release no matter which Jira project the ticket key comes from.

    The published commit is recorded in the nuspec <repository commit="..."> field and in the
    assembly informational version, and the same commit is tagged with the version, so every
    package on the feed can be traced back to its source.

    Publishing is refused from an uncommitted tree - otherwise the recorded commit would be a
    lie about what is inside the package.

.PARAMETER Feed
    NuGet feed to push to. Defaults to the internal Mews Azure Artifacts feed.

.PARAMETER ReleaseBranch
    Branch that produces `rel` versions. Defaults to `mews`.

.PARAMETER DryRun
    Pack only - skip pushing the packages and skip tagging.

.EXAMPLE
    ./mews-internal-release.ps1
    Publishes from the current branch and tags the commit.

.EXAMPLE
    ./mews-internal-release.ps1 -DryRun
    Builds the packages into ./artifacts so they can be inspected, without publishing.
#>

[CmdletBinding()]
param(
    [string]$Feed = 'https://pkgs.dev.azure.com/mews/_packaging/mews/nuget/v3/index.json',
    [string]$ReleaseBranch = 'mews',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Invoke-Native {
    param([Parameter(Mandatory)][ScriptBlock]$Command)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $($Command.ToString().Trim())"
    }
}

function Get-VersionPrefix {
    param([Parameter(Mandatory)][String]$PropsPath)

    if (-not (Test-Path $PropsPath)) {
        throw "Cannot determine the version prefix - $PropsPath not found."
    }

    $prefix = @(Select-Xml -Path $PropsPath -XPath '/Project/PropertyGroup/VersionPrefix') |
        Select-Object -First 1 -ExpandProperty Node |
        Select-Object -ExpandProperty InnerText
    if ([String]::IsNullOrWhiteSpace($prefix)) {
        throw "Cannot determine the version prefix - no <VersionPrefix> in $PropsPath."
    }

    return $prefix.Trim()
}

# Branch builds get a leading `dev` identifier so they always sort below `rel`, whatever the
# ticket key is. The ticket key follows it purely to say where the build came from.
function Get-VersionLabel {
    param([Parameter(Mandatory)][String]$Branch)

    if ($Branch -eq $ReleaseBranch) {
        return 'rel'
    }

    $ticket = [Regex]::Match($Branch, '[A-Za-z][A-Za-z0-9]*-[0-9]+')
    if ($ticket.Success) {
        $source = $ticket.Value.ToUpperInvariant()
    }
    else {
        $source = ($Branch -replace '[^0-9A-Za-z-]', '-').Trim('-').ToUpperInvariant()
    }

    if ([String]::IsNullOrEmpty($source)) {
        throw "Cannot derive a version label from branch '$Branch'."
    }

    return "dev.$source"
}

$repositoryRoot = $PSScriptRoot
Push-Location $repositoryRoot
try {
    $dirty = Invoke-Native { git status --porcelain }
    if ($dirty) {
        throw "Refusing to publish from an uncommitted tree. Commit or stash first:`n$($dirty -join [Environment]::NewLine)"
    }

    $branch = (Invoke-Native { git rev-parse --abbrev-ref HEAD }).Trim()
    if ($branch -eq 'HEAD') {
        throw 'Refusing to publish from a detached HEAD - check out a branch so the version label can be derived.'
    }

    $commit = (Invoke-Native { git rev-parse HEAD }).Trim()
    if (-not (Invoke-Native { git branch --remotes --contains $commit })) {
        Write-Warning "Commit $commit is not on any remote branch yet. It will only be reachable through the tag this script pushes."
    }

    $versionPrefix = Get-VersionPrefix -PropsPath (Join-Path $repositoryRoot 'src/Directory.Build.props')
    $label = Get-VersionLabel -Branch $branch
    $timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmm')
    $version = "$versionPrefix-$label.$timestamp"
    $tag = $version

    if (Invoke-Native { git tag --list $tag }) {
        throw "Tag $tag already exists - a release was published in the same minute. Wait a minute and retry."
    }

    Write-Host "Version: $version"
    Write-Host "Branch:  $branch"
    Write-Host "Commit:  $commit"

    $projects = @('MiniCover', 'MiniCover.Core', 'MiniCover.HitServices', 'MiniCover.Reports')
    $output = Join-Path $repositoryRoot 'artifacts'
    if (Test-Path $output) {
        Remove-Item $output -Recurse -Force
    }
    New-Item -ItemType Directory -Path $output | Out-Null

    foreach ($project in $projects) {
        $projectPath = Join-Path 'src' $project
        Invoke-Native {
            dotnet pack $projectPath `
                --configuration Release `
                --output $output `
                "/p:Version=$version" `
                "/p:RepositoryCommit=$commit" `
                "/p:RepositoryBranch=$branch" `
                "/p:SourceRevisionId=$commit"
        }
    }

    $packages = @(Get-ChildItem -Path $output -Filter *.nupkg)
    if ($packages.Count -ne $projects.Count) {
        throw "Expected $($projects.Count) packages in $output but found $($packages.Count)."
    }

    if ($DryRun) {
        Write-Host "Dry run - packages are in $output, nothing was published and no tag was created."
        return
    }

    foreach ($package in $packages) {
        Invoke-Native { dotnet nuget push $package.FullName --source $Feed --api-key az }
    }

    Invoke-Native { git tag --annotate $tag --message "Mews.MiniCover $version" $commit }
    Invoke-Native { git push origin $tag }

    Write-Host "Published $version and tagged $commit."
}
finally {
    Pop-Location
}
