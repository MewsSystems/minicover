using System;
using System.Collections.Generic;
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
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace MiniCover.Core.UnitTests.Instrumentation
{
    // Exercises the real, DI-wired AssemblyInstrumenter (as production code assembles it via
    // AddMiniCoverCore) against a small fixture assembly compiled on the fly, so each skip reason
    // can be triggered directly instead of relying on real repository source files.
    public class AssemblyInstrumenterTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _foreignDir;
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

            var runId = $"{Environment.CurrentManagedThreadId}-{Environment.TickCount64}";
            _tempDir = Path.Join(Path.GetTempPath(), $"minicover-tests-{runId}");
            Directory.CreateDirectory(_tempDir);

            // Outside _tempDir (our Workdir), simulating a third-party assembly's document.
            _foreignDir = Path.Join(Path.GetTempPath(), $"minicover-tests-{runId}-foreign");
            Directory.CreateDirectory(_foreignDir);
        }

        public void Dispose()
        {
            Directory.Delete(_tempDir, true);
            Directory.Delete(_foreignDir, true);
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
        public void WithSourceOutsideWorkdir_ReturnsNothingToInstrumentSkip()
        {
            var (assemblyFile, _) = CompileFixtureAssembly("Fixture5", sourceDir: _foreignDir);

            var context = CreateContext(Array.Empty<string>());

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.Assembly.Should().BeNull();
            outcome.SkipReason.Should().Be(InstrumentationSkipReason.NothingToInstrument);
        }

        [Fact]
        public void WhenOwnSourceFileDeletedSinceCompilation_ReturnsSourceFilesChangedSkip()
        {
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture6");
            File.Delete(sourceFile.FullName);

            var context = CreateContext(new[] { sourceFile.FullName });

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.Assembly.Should().BeNull();
            outcome.SkipReason.Should().Be(InstrumentationSkipReason.SourceFilesChanged);
        }

        // The F# compiler in .NET SDK 10.0.400 emits a checksum-less document named 'unknown'
        // alongside the real ones. It matches no file, so it must not make the assembly look stale.
        [Fact]
        public void WithChecksumlessDocument_ReturnsInstrumentedOutcome()
        {
            var checksumlessDocumentPath = Path.Join(_tempDir, "unknown");
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture7", extraDocumentPath: checksumlessDocumentPath);
            var strippedAssemblyFile = StripDocumentChecksum(assemblyFile, checksumlessDocumentPath);

            var context = CreateContext(new[] { sourceFile.FullName });

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, strippedAssemblyFile);

            outcome.SkipReason.Should().BeNull();
            outcome.Assembly.Should().NotBeNull();
            outcome.Assembly.Methods.Should().NotBeEmpty();
        }

        // Same assembly shape, but with the checksum left in place - the guarantee that a
        // checksummed document missing from disk still skips the assembly.
        [Fact]
        public void WithChecksummedDocumentMissingFromDisk_ReturnsSourceFilesChangedSkip()
        {
            var missingDocumentPath = Path.Join(_tempDir, "unknown");
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture8", extraDocumentPath: missingDocumentPath);

            var context = CreateContext(new[] { sourceFile.FullName });

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.Assembly.Should().BeNull();
            outcome.SkipReason.Should().Be(InstrumentationSkipReason.SourceFilesChanged);
        }

        // A #line directive makes the compiler emit a document for a file it never read, so that
        // document has no checksum and no file - the same shape as an F# 'unknown' document, but
        // reachable with any compiler. Here the fileless document's sequence points sit inside a
        // method that is instrumented, because the method's other documents are ours.
        [Fact]
        public void WithDocumentFromLineDirective_ReturnsInstrumentedOutcome()
        {
            var (assemblyFile, sourceFile) = CompileFixtureAssembly("Fixture9", lineDirectiveTarget: "Template.tt");

            var context = CreateContext(new[] { sourceFile.FullName });

            var outcome = _assemblyInstrumenter.InstrumentAssemblyFile(context, assemblyFile);

            outcome.SkipReason.Should().BeNull();
            outcome.Assembly.Should().NotBeNull();
            outcome.Assembly.Methods.Should().NotBeEmpty();
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

        // Rewrites the assembly with one document's checksum removed, the way a compiler that emits a
        // sentinel document does. Roslyn cannot emit that shape itself - it refuses to emit debug
        // information for source text it has no checksum for.
        private IFileInfo StripDocumentChecksum(IFileInfo assemblyFile, string documentPath)
        {
            var strippedAssemblyPath = Path.Join(_tempDir, $"{Path.GetFileNameWithoutExtension(assemblyFile.Name)}.Stripped.dll");

            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFile.FullName, new ReaderParameters { ReadSymbols = true }))
            {
                var documents = assemblyDefinition.MainModule.GetTypes()
                    .SelectMany(type => type.Methods)
                    .Where(method => method.DebugInformation != null)
                    .SelectMany(method => method.DebugInformation.SequencePoints)
                    .Select(sequencePoint => sequencePoint.Document)
                    .Where(document => document.Url == documentPath)
                    .ToArray();

                documents.Should().NotBeEmpty(because: $"the fixture assembly should have a document for {documentPath}");

                foreach (var document in documents)
                {
                    document.HashAlgorithm = DocumentHashAlgorithm.None;
                    document.Hash = Array.Empty<byte>();
                }

                assemblyDefinition.Write(strippedAssemblyPath, new WriterParameters { WriteSymbols = true });
            }

            return _fileSystem.FileInfo.New(strippedAssemblyPath);
        }

        private (IFileInfo assemblyFile, IFileInfo sourceFile) CompileFixtureAssembly(
            string className,
            string sourceDir = null,
            string extraDocumentPath = null,
            string lineDirectiveTarget = null)
        {
            var sourcePath = Path.Join(sourceDir ?? _tempDir, $"{className}.cs");
            var assemblyPath = Path.Join(_tempDir, $"{className}.dll");
            var pdbPath = Path.Join(_tempDir, $"{className}.pdb");

            // File.WriteAllText and CSharpSyntaxTree.ParseText must agree on encoding (no BOM), otherwise
            // the checksum embedded in the PDB won't match the one FileHasChanged() computes from disk.
            var noBomUtf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            var body = lineDirectiveTarget == null
                ? """
                        int x = 1;
                        if (x > 0)
                        {
                            x++;
                        }
                """
                : $$"""
                        int x = 1;
                #line 42 "{{lineDirectiveTarget}}"
                        if (x > 0)
                        {
                            x++;
                        }
                #line default
                """;

            File.WriteAllText(sourcePath, $$"""
                public class {{className}}
                {
                    public void Run()
                    {
                {{body}}
                    }
                }
                """, noBomUtf8);

            var syntaxTrees = new List<SyntaxTree>
            {
                CSharpSyntaxTree.ParseText(
                    File.ReadAllText(sourcePath, noBomUtf8),
                    path: sourcePath,
                    encoding: noBomUtf8)
            };

            // An extra document that is never written to disk, so the emitted PDB carries a document
            // matching no file - the shape a compiler-generated or sentinel document has.
            if (extraDocumentPath != null)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                    $"public class {className}Extra {{ public void Run() {{ int x = 1; }} }}",
                    path: extraDocumentPath,
                    encoding: noBomUtf8));
            }

            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

            var compilation = CSharpCompilation.Create(
                className,
                syntaxTrees,
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
