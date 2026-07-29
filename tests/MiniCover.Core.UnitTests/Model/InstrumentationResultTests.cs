using FluentAssertions;
using MiniCover.Core.Model;
using Xunit;

namespace MiniCover.UnitTests.Model
{
    public class InstrumentationResultTests
    {
        [Fact]
        public void NewResult_HasNoAssembliesExtraAssembliesOrSkippedAssemblies()
        {
            var result = new InstrumentationResult();

            result.Assemblies.Should().BeEmpty();
            result.ExtraAssemblies.Should().BeEmpty();
            result.SkippedAssemblies.Should().BeEmpty();
        }

        [Fact]
        public void AddInstrumentedAssembly_AddsToAssemblies()
        {
            var result = new InstrumentationResult();
            var assembly = new InstrumentedAssembly("Sample");

            result.AddInstrumentedAssembly(assembly);

            result.Assemblies.Should().ContainSingle().Which.Should().BeSameAs(assembly);
        }

        [Fact]
        public void AddExtraAssembly_AddsToExtraAssemblies()
        {
            var result = new InstrumentationResult();

            result.AddExtraAssembly("/path/to/extra.dll");

            result.ExtraAssemblies.Should().ContainSingle().Which.Should().Be("/path/to/extra.dll");
        }

        [Fact]
        public void AddSkippedAssembly_AddsToSkippedAssembliesWithFileAndReason()
        {
            var result = new InstrumentationResult();

            result.AddSkippedAssembly("/path/to/skipped.dll", InstrumentationSkipReason.SourceFilesChanged);

            var skipped = result.SkippedAssemblies.Should().ContainSingle().Subject;
            skipped.AssemblyFile.Should().Be("/path/to/skipped.dll");
            skipped.Reason.Should().Be(InstrumentationSkipReason.SourceFilesChanged);
        }

        [Fact]
        public void AddSkippedAssembly_MultipleCalls_AreAllTracked()
        {
            var result = new InstrumentationResult();

            result.AddSkippedAssembly("/path/to/a.dll", InstrumentationSkipReason.SourceFilesChanged);
            result.AddSkippedAssembly("/path/to/b.dll", InstrumentationSkipReason.NothingToInstrument);

            result.SkippedAssemblies.Should().HaveCount(2);
        }
    }
}
