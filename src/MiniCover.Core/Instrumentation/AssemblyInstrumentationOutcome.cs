using MiniCover.Core.Model;

namespace MiniCover.Core.Instrumentation
{
    public class AssemblyInstrumentationOutcome
    {
        private AssemblyInstrumentationOutcome(InstrumentedAssembly assembly, InstrumentationSkipReason? skipReason)
        {
            Assembly = assembly;
            SkipReason = skipReason;
        }

        public InstrumentedAssembly Assembly { get; }
        public InstrumentationSkipReason? SkipReason { get; }

        public static AssemblyInstrumentationOutcome Instrumented(InstrumentedAssembly assembly)
        {
            return new AssemblyInstrumentationOutcome(assembly, null);
        }

        public static AssemblyInstrumentationOutcome Skipped(InstrumentationSkipReason reason)
        {
            return new AssemblyInstrumentationOutcome(null, reason);
        }
    }
}
