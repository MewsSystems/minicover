using MiniCover.Core.Model;

namespace MiniCover.Core.Extensions
{
    public static class InstrumentationSkipReasonExtensions
    {
        /// <summary>
        /// True for skip reasons that indicate something unexpected happened (e.g. source files
        /// changed during instrumentation), as opposed to the normal, harmless cases of an
        /// assembly with no matching local source or one that was already instrumented.
        /// </summary>
        public static bool IndicatesInstrumentationProblem(this InstrumentationSkipReason reason)
        {
            return reason == InstrumentationSkipReason.SourceFilesChanged
                || reason == InstrumentationSkipReason.InvalidAssemblyFormat;
        }
    }
}
