using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyInjection;
using MiniCover.Core.Extensions;
using MiniCover.Core.Instrumentation;
using MiniCover.Core.Model;
using Xunit;

namespace MiniCover.Core.UnitTests.Instrumentation
{
    // Exercises the real, DI-wired AssemblyInstrumenter (as production code assembles it via
    // AddMiniCoverCore) against a small fixture assembly compiled on the fly, so each skip reason
    // can be triggered directly instead of relying on real repository source files.
    public class AssemblyInstrumenterTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly IFileSystem _fileSystem;
        private readonly IAssemblyInstrumenter _assemblyInstrumenter;

        public AssemblyInstrumenterTests()
        {
            var serviceProvider = new ServiceCollection()
                .AddMiniCoverCore()
                .AddLogging()
                .BuildServiceProvider();

            _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
            _assemblyInstrumenter = serviceProvider.GetRequiredService<IAssemblyInstrumenter>();

            _tempDir = Path.Join(Path.GetTempPath(), $"minicover-tests-{Environment.CurrentManagedThreadId}-{Environment.TickCount64}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            Directory.Delete(_tempDir, true);
        }

        [Fact]
        public void WithMatchingSource_ReturnsInstrumentedOutcome()
        {
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture1");

            var context = CreateContext(new[] { sourceFile.FullName });

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.SkipReason.Should().BeNull();
            outcome.Assembly.Should().NotBeNull();
            outcome.Assembly.Methods.Should().NotBeEmpty();
            outcome.Assembly.TempAssemblyFile.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void WithNoMatchingSource_ReturnsNothingToInstrumentSkip()
        {
            var (assemblyFile, _) = CompileFixtureAssembly("Fixture2");

            var context = CreateContext(Array.Empty<string>());

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.Assembly.Should().BeNull();
            outcome.SkipReason.Should().Be(InstrumentationSkipReason.NothingToInstrument);
        }

        [Fact]
        public void WhenSourceFileChangedSinceCompilation_ReturnsSourceFilesChangedSkip()
        {
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture3");

            File.WriteAllText(sourceFile.FullName, "public class Fixture3 { public void Run() { int x = 2; } }");

            var context = CreateContext(new[] { sourceFile.FullName });

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.Assembly.Should().BeNull();
            outcome.SkipReason.Should().Be(InstrumentationSkipReason.SourceFilesChanged);
        }

        [Fact]
        public void WhenAlreadyInstrumented_ReturnsAlreadyInstrumentedSkip()
        {
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture4");

            var context = CreateContext(new[] { sourceFile.FullName });

            var firstOutcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);
            firstOutcome.Assembly.Should().NotBeNull();

            var instrumentedAssemblyFile = _fileSystem.FileInfo.New(firstOutcome.Assembly.TempAssemblyFile);

            var secondOutcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, instrumentedAssemblyFile);

            secondOutcome.Assembly.Should().BeNull();
            secondOutcome.SkipReason.Should().Be(InstrumentationSkipReason.AlreadyInstrumented);
        }

        private (IFileInfo assemblyFile, IFileInfo sourceFile) CompileFixtureAssembly(string className)
        {
            var sourcePath = Path.Join(_tempDir, $"{className}.cs");
            var assemblyPath = Path.Join(_tempDir, $"{className}.dll");
            var pdbPath = Path.Join(_tempDir, $"{className}.pdb");

            // File.WriteAllText and CSharpSyntaxTree.ParseText must agree on encoding (no BOM), otherwise
            // the checksum embedded in the PDB won't match the one FileHasChanged() computes from disk.
            var noBomUtf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            File.WriteAllText(sourcePath, $$"""
                public class {{className}}
                {
                    public void Run()
                    {
                        int x = 1;
                        if (x > 0)
                        {
                            x++;
                        }
                    }
                }
                """, noBomUtf8);

            var syntaxTree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(sourcePath, noBomUtf8),
                path: sourcePath,
                encoding: noBomUtf8);

            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

            var compilation = CSharpCompilation.Create(
                className,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = File.Create(assemblyPath))
            using (var pdbStream = File.Create(pdbPath))
            {
                var emitResult = compilation.Emit(
                    assemblyStream,
                    pdbStream,
                    options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

                emitResult.Success.Should().BeTrue(because: string.Join(Environment.NewLine, emitResult.Diagnostics));
            }

            return (_fileSystem.FileInfo.New(assemblyPath), _fileSystem.FileInfo.New(sourcePath));
        }

        private FileBasedInstrumentationContext CreateContext(string[] sources)
        {
            return new FileBasedInstrumentationContext
            {
                Assemblies = Array.Empty<IFileInfo>(),
                Sources = sources.Select(s => _fileSystem.FileInfo.New(s)).ToArray(),
                Tests = Array.Empty<IFileInfo>(),
                HitsPath = _tempDir,
                Workdir = _fileSystem.DirectoryInfo.New(_tempDir)
            };
        }
    }
}
