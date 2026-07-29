using MiniCover.Core.Model;

namespace MiniCover.Core.Extensions
{
    public static class InstrumentationSkipReasonExtensions
    {
        /// <summary>
        /// True for skip reasons that indicate something unexpected happened, as opposed to the
        /// normal, harmless cases of an assembly with no matching local source or one that was
        /// already instrumented. Deliberately an allow-list of the benign reasons rather than a
        /// deny-list of the problem ones, so a future InstrumentationSkipReason value fails loud
        /// by default instead of silently being treated as safe.
        /// </summary>
        public static bool IndicatesInstrumentationProblem(this InstrumentationSkipReason reason)
        {
            return reason != InstrumentationSkipReason.AlreadyInstrumented
                && reason != InstrumentationSkipReason.NothingToInstrument;
        }
    }
}
