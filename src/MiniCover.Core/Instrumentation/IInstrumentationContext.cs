using System.Collections.Generic;
using System.IO.Abstractions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MiniCover.Core.Instrumentation
{
    public interface IInstrumentationContext
    {
        IList<IFileInfo> Assemblies { get; }
        string HitsPath { get; }
        IDirectoryInfo Workdir { get; }
        int NewInstructionId();
        bool IsSource(MethodDefinition methodDefinition);
        bool IsTest(MethodDefinition methodDefinition);

        /// <summary>
        /// True if this document's path falls under our workdir, as opposed to a document
        /// belonging to a third-party assembly's own build (e.g. a NuGet package's portable PDB
        /// referencing a path from the package author's machine, which is never part of our
        /// checkout). Existence-independent by design: a document we own can go missing (a real
        /// problem) and must still count as ours, while a foreign path never does regardless of
        /// whether something happens to exist at that location.
        /// </summary>
        bool IsKnownDocument(Document document);
    }
}
