using System.IO.Abstractions;

namespace MiniCover.Core.Instrumentation
{
    public interface IAssemblyInstrumenter
    {
        AssemblyInstrumentationOutcome InstrumentAssemblyFile(IInstrumentationContext context, IFileInfo assemblyFile);
    }
}