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
        /// True if this document is one of our tracked Sources or Tests files, as opposed to a
        /// document belonging to a third-party assembly's own build (e.g. a NuGet package's
        /// portable PDB referencing paths from the package author's machine, which never exist
        /// locally). Used to scope the "has this source changed" check to files we can
        /// meaningfully compare, instead of treating every unmatched foreign path as changed.
        /// </summary>
        bool IsKnownDocument(Document document);
    }
}
