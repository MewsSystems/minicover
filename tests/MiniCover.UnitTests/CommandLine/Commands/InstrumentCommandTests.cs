using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MiniCover.CommandLine.Options;
using MiniCover.Commands;
using MiniCover.Core.Instrumentation;
using MiniCover.Core.Model;
using MiniCover.IO;
using Moq;
using Xunit;

namespace MiniCover.UnitTests.CommandLine.Commands
{
    public class InstrumentCommandTests : CommandTests<InstrumentCommand>, IDisposable
    {
        private readonly string _tempDir;
        private readonly Mock<IInstrumenter> _instrumenter;
        private readonly FailOnSkippedAssembliesOption _failOnSkippedAssembliesOption;

        public InstrumentCommandTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"minicover-tests-{Environment.CurrentManagedThreadId}-{Environment.TickCount64}");
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, "a.dll"), "dummy");
            File.WriteAllText(Path.Combine(_tempDir, "a.cs"), "class C {}");

            var fileSystem = new FileSystem();

            var outputMock = new Mock<IOutput>();
            outputMock.SetupProperty(o => o.MinimumLevel, LogLevel.Information);
            var verbosityOption = new VerbosityOption(outputMock.Object);
            verbosityOption.ReceiveValue(null);

            var workingDirectoryOption = new WorkingDirectoryOption(NullLogger<WorkingDirectoryOption>.Instance, fileSystem);
            workingDirectoryOption.ReceiveValue(Directory.GetCurrentDirectory());

            var parentDirOption = new ParentDirectoryOption(fileSystem);
            parentDirOption.ReceiveValue(_tempDir);

            var includeAssembliesOption = new IncludeAssembliesPatternOption();
            includeAssembliesOption.ReceiveValue(new[] { "*.dll" });
            var excludeAssembliesOption = new ExcludeAssembliesPatternOption();
            excludeAssembliesOption.ReceiveValue(null);

            var includeSourceOption = new IncludeSourcesPatternOption();
            includeSourceOption.ReceiveValue(new[] { "*.cs" });
            var excludeSourceOption = new ExcludeSourcesPatternOption();
            excludeSourceOption.ReceiveValue(null);

            var includeTestsOption = new IncludeTestsPatternOption();
            includeTestsOption.ReceiveValue(null);
            var excludeTestsOption = new ExcludeTestsPatternOption();
            excludeTestsOption.ReceiveValue(null);

            var hitsDirectoryOption = new HitsDirectoryOption(fileSystem);
            hitsDirectoryOption.ReceiveValue(Path.Combine(_tempDir, "coverage-hits"));

            var coverageFileOption = new CoverageFileOption(fileSystem);
            coverageFileOption.ReceiveValue(Path.Combine(_tempDir, "coverage.json"));

            _failOnSkippedAssembliesOption = new FailOnSkippedAssembliesOption();

            _instrumenter = MockFor<IInstrumenter>();

            Sut = new InstrumentCommand(
                Mock.Of<IServiceProvider>(),
                verbosityOption,
                workingDirectoryOption,
                parentDirOption,
                includeAssembliesOption,
                excludeAssembliesOption,
                includeSourceOption,
                excludeSourceOption,
                includeTestsOption,
                excludeTestsOption,
                hitsDirectoryOption,
                coverageFileOption,
                _failOnSkippedAssembliesOption,
                _instrumenter.Object,
                NullLogger<InstrumentCommand>.Instance
            );
        }

        public new void Dispose()
        {
            Directory.Delete(_tempDir, true);
            base.Dispose();
        }

        [Fact]
        public async Task WithoutFlag_ReturnsZero_EvenWhenAssembliesWereSkippedForAProblem()
        {
            var result = new InstrumentationResult();
            result.AddSkippedAssembly("a.dll", InstrumentationSkipReason.SourceFilesChanged);
            _instrumenter.Setup(x => x.Instrument(It.IsAny<IInstrumentationContext>())).Returns(result);

            var exitCode = await Sut.Execute();

            exitCode.Should().Be(0);
        }

        [Fact]
        public async Task WithFlag_ReturnsZero_WhenNoAssembliesWereSkipped()
        {
            _failOnSkippedAssembliesOption.ReceiveValue(true);

            var result = new InstrumentationResult();
            _instrumenter.Setup(x => x.Instrument(It.IsAny<IInstrumentationContext>())).Returns(result);

            var exitCode = await Sut.Execute();

            exitCode.Should().Be(0);
        }

        [Fact]
        public async Task WithFlag_ReturnsZero_WhenSkippedAssembliesAreOnlyBenign()
        {
            _failOnSkippedAssembliesOption.ReceiveValue(true);

            var result = new InstrumentationResult();
            result.AddSkippedAssembly("nunit.dll", InstrumentationSkipReason.NothingToInstrument);
            result.AddSkippedAssembly("a.dll", InstrumentationSkipReason.AlreadyInstrumented);
            _instrumenter.Setup(x => x.Instrument(It.IsAny<IInstrumentationContext>())).Returns(result);

            var exitCode = await Sut.Execute();

            exitCode.Should().Be(0);
        }

        [Fact]
        public async Task WithFlag_ReturnsOne_WhenAnAssemblyWasSkippedBecauseItsSourceChanged()
        {
            _failOnSkippedAssembliesOption.ReceiveValue(true);

            var result = new InstrumentationResult();
            result.AddSkippedAssembly("a.dll", InstrumentationSkipReason.SourceFilesChanged);
            _instrumenter.Setup(x => x.Instrument(It.IsAny<IInstrumentationContext>())).Returns(result);

            var exitCode = await Sut.Execute();

            exitCode.Should().Be(1);
        }

        [Fact]
        public async Task WithFlag_ReturnsOne_WhenAnAssemblyHadAnInvalidFormat()
        {
            _failOnSkippedAssembliesOption.ReceiveValue(true);

            var result = new InstrumentationResult();
            result.AddSkippedAssembly("a.dll", InstrumentationSkipReason.InvalidAssemblyFormat);
            _instrumenter.Setup(x => x.Instrument(It.IsAny<IInstrumentationContext>())).Returns(result);

            var exitCode = await Sut.Execute();

            exitCode.Should().Be(1);
        }
    }
}
