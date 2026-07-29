using FluentAssertions;
using MiniCover.Core.Extensions;
using MiniCover.Core.Model;
using Xunit;

namespace MiniCover.UnitTests.Extensions
{
    public class InstrumentationSkipReasonExtensionsTests
    {
        [InlineData(InstrumentationSkipReason.AlreadyInstrumented, false)]
        [InlineData(InstrumentationSkipReason.NothingToInstrument, false)]
        [InlineData(InstrumentationSkipReason.SourceFilesChanged, true)]
        [InlineData(InstrumentationSkipReason.InvalidAssemblyFormat, true)]
        [Theory]
        public void IndicatesInstrumentationProblem(InstrumentationSkipReason reason, bool expected)
        {
            reason.IndicatesInstrumentationProblem().Should().Be(expected);
        }
    }
}
