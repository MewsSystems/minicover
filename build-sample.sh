#!/usr/bin/env bash

set -e

# The package version is fixed at 1.0.0, so the cached copy has to go or the sample would keep
# restoring whichever build got there first. Ask nuget where its cache is rather than assuming
# ~/.nuget, which a configured globalPackagesFolder overrides.
globalPackages=$(dotnet nuget locals global-packages --list | sed 's/^global-packages: //')
rm -rf "$globalPackages/mews.minicover/1.0.0" || true
dotnet pack -c Release --output $PWD/sample/nupkgs
cd sample
rm -rf ./coverage
dotnet build
dotnet tool restore -v q
dotnet minicover reset
echo "# Start Instrument"
# --fail-on-skipped-assemblies so an assembly that silently drops out of instrumentation fails
# here, instead of surfacing later as coverage drifting under the report threshold.
dotnet minicover instrument --fail-on-skipped-assemblies
echo "# End Instrument"
dotnet test --no-build
echo "# Start Uninstrument"
dotnet minicover uninstrument
echo "# End Uninstrument"
echo "# Start Report"
dotnet minicover report --threshold 60
echo "# End Report"
echo "# Start HtmlReport"
dotnet minicover htmlreport --threshold 60
echo "# End HtmlReport"
echo "# Start XmlReport"
dotnet minicover xmlreport
echo "# End XmlReport"
echo "# Start OpenCoverReport"
dotnet minicover opencoverreport
echo "# End OpenCoverReport"
echo "# Start CloverReport"
dotnet minicover cloverreport
echo "# End CloverReport"
echo "# Start CoberturaReport"
dotnet minicover coberturareport
echo "# End CoberturaReport"
cd ..
