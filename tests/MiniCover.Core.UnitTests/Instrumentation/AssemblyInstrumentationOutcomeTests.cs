using FluentAssertions;
using MiniCover.Core.Instrumentation;
using MiniCover.Core.Model;
using Xunit;

namespace MiniCover.Core.UnitTests.Instrumentation
{
    public class AssemblyInstrumentationOutcomeTests
    {
        [Fact]
        public void Instrumented_ExposesAssemblyAndNoSkipReason()
        {
            var assembly = new InstrumentedAssembly("Sample");

            var outcome = AssemblyInstrumentationOutcome.Instrumented(assembly);

            outcome.Assembly.Should().BeSameAs(assembly);
            outcome.SkipReason.Should().BeNull();
        }

        [Fact]
        public void Skipped_ExposesReasonAndNoAssembly()
        {
            var outcome = AssemblyInstrumentationOutcome.Skipped(InstrumentationSkipReason.SourceFilesChanged);

            outcome.Assembly.Should().BeNull();
            outcome.SkipReason.Should().Be(InstrumentationSkipReason.SourceFilesChanged);
        }
    }
}
