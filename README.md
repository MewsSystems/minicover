# MiniCover
Code Coverage Tool for .NET Core

[![Build Status](https://dev.azure.com/lucaslorentzlara/lucaslorentzlara/_apis/build/status/lucaslorentz.minicover?branchName=master)](https://dev.azure.com/lucaslorentzlara/lucaslorentzlara/_build/latest?definitionId=3&branchName=master)
[![Nuget](https://img.shields.io/nuget/v/minicover)](https://www.nuget.org/packages/MiniCover/)
[![Coverage Status](https://coveralls.io/repos/github/lucaslorentz/minicover/badge.svg?branch=master)](https://coveralls.io/github/lucaslorentz/minicover?branch=master)

## Supported .NET Core SDKs
- 8.0 (Global tool or local tool)
- 9.0 (Global tool or local tool)
- 10.0 (Global tool or local tool)

## Installation
MiniCover can be installed as a global tool:
```
dotnet tool install --global minicover
```
Or local tool:
```
dotnet tool install minicover
```

## Commands
This is a simplified documentation of MiniCover commands and options.

Use `--help` for more information:
```
minicover --help
```

**When installed as local tool, MiniCover commands must be prefixed with `dotnet`.** Example:
```
dotnet minicover --help
```

### Instrument
```
minicover instrument
```

Use this command to instrument assemblies to record code coverage.

It is based on the following main options:

|option|description|type|default|
|-|-|-|-|
|**sources**|source files to track coverage|glob|`src/**/*.cs`|
|**exclude-sources**|exceptions to source option|glob|`**/bin/**/*.cs` and `**/obj/**/*.cs`|
|**tests**|test files used to recognize test methods|glob|`tests/**/*.cs` and `test/**/*.cs`|
|**exclude-tests**|exceptions to tests option|glob|`**/bin/**/*.cs` and `**/obj/**/*.cs`|
|**assemblies**|assemblies considered for instrumentation|glob|`**/*.dll`|
|**exclude-assemblies**|Exceptions to assemblies option|glob|`**/obj/**/*.dll`|
|**fail-on-skipped-assemblies**|Fail this command (non-zero exit code) if some assemblies could not be instrumented for an unexpected reason, e.g. their source files changed while instrumenting|flag|`false`|

*Note 1: Assemblies not related to sources or tests are automatically ignored.*

*Note 2: [Supported syntax](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing.matcher?view=dotnet-plat-ext-3.0#remarks) for glob values.*

*Note 3: You can repeat glob options to represent multiple values. Example: `--sources "a/**/*.cs" --sources "b/**/*.cs"`*

This command also generates a **coverage.json** file with information about the instrumented code.   

### Uninstrument
```
minicover uninstrument
````

Use this command to revert the instrumentation based on **coverage.json** file.

**Make sure you call uninstrument before publishing or packing your application.**

### Reset
```
minicover reset
````

Use this command to reset the recorded coverage so far.

### Report
```
minicover report
````

Use this command to print a coverage report in the console.

The command exits with failure if the coverage doesn't meet a specific threshold (90% by default).

### More commands

- **cloverreport**: Write an Clover-formatted XML report to file
- **coberturareport**: Write a cobertura XML report to file
- **coverallsreport**: Prepare and/or submit coveralls reports
- **htmlreport**: Write html report to folder
- **opencoverreport**: Write an OpenCover-formatted XML report to file
- **xmlreport**: Write an NCover-formatted XML report to file

Use `--help` for more information.

## Build script example
```shell
dotnet restore
dotnet build

# Instrument
minicover instrument

# Reset hits
minicover reset

dotnet test --no-build

# Uninstrument
minicover uninstrument

# Create html reports inside folder coverage-html
minicover htmlreport --threshold 90

# Console report
minicover report --threshold 90
```

## Ignore coverage files

Add the following to your .gitignore file to ignore code coverage results:
```
coverage-html
coverage-hits
coverage.json
```

## Libraries

When using MiniCover libraries:

- use dependency injection to create instances
- use only the main interfaces listed on each package below

By doing that, you reduce the risk of being impacted by future MiniCover changes.

### MiniCover.Core

Main MiniCover operations.

Dependency injection configuration:
```C#
services.AddMiniCoverCore();
```

Main interfaces:
- IInstrumenter
- IUninstrumenter
- IHitsReader
- IHitsResetter

### MiniCover.Reports

MiniCover reports.

Dependency injection configuration:
```C#
services.AddMiniCoverCore();
services.AddMiniCoverReports();
```

Main interfaces:
- ICloverReport
- ICoberturaReport
- IConsoleReport
- ICoverallsReport
- IHtmlReport
- IHtmlSourceFileReport
- INCoverReport
- IOpenCoverReport

## Releasing (Mews fork)

This fork publishes `Mews.MiniCover*` packages to the internal Azure Artifacts feed with
[mews-internal-release.ps1](mews-internal-release.ps1). Versions are derived by the script, never
hand-written:

|branch|version|
|-|-|
|`mews`|`3.9.0-rel.202608131409`|
|anything else|`3.9.0-dev.MOD-348.202608131409`|

- `3.9.0` is the upstream version the fork is based on. It lives in
  [src/Directory.Build.props](src/Directory.Build.props) and is the only place to change it when the
  fork is rebased onto a newer upstream.
- `rel` / `dev` says whether it is a release or a branch build. NuGet compares pre-release labels
  case-insensitively, one dot-separated identifier at a time — `dev` < `rel` and it comes first, so
  a branch build can never look newer than a release, whatever the ticket key is.
- The ticket key, taken from the branch name, says where a branch build came from.
- The timestamp is UTC `yyyyMMddHHmm`, so versions order within each line and nobody has to look up
  what was published last.

To publish:

```powershell
./mews-internal-release.ps1
```

The script refuses to run on an uncommitted tree, records the published commit in the nuspec
`<repository commit="...">` field and in the assembly informational version, and tags that commit
with the version it published. Use `-DryRun` to build the packages into `./artifacts` without
publishing or tagging.

**Consumers must pin an exact version.** A floating `3.9.0-*` range resolves to unmerged branch
builds.
