using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using MiniCover.Core.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MiniCover.Core.Instrumentation
{
    public class FileBasedInstrumentationContext : IInstrumentationContext
    {
        private int _uniqueId;

        public virtual IList<IFileInfo> Assemblies { get; set; }
        public virtual IList<IFileInfo> Sources { get; set; }
        public virtual IList<IFileInfo> Tests { get; set; }
        public virtual string HitsPath { get; set; }
        public virtual IDirectoryInfo Workdir { get; set; }

        public virtual int NewInstructionId()
        {
            return ++_uniqueId;
        }

        public virtual bool IsSource(MethodDefinition methodDefinition)
        {
            return methodDefinition.GetAllDocuments()
                .Any(d => Sources.Any(s => s.FullName == d.Url));
        }

        public virtual bool IsTest(MethodDefinition methodDefinition)
        {
            return methodDefinition.GetAllDocuments()
                .Any(d => Tests.Any(s => s.FullName == d.Url));
        }

        public virtual bool IsKnownDocument(Document document)
        {
            // Membership in Sources/Tests can't be used here: those lists are built by globbing
            // files that currently exist on disk, so a document belonging to our own workdir that
            // has simply gone missing (the actual incident this ticket is about) would wrongly
            // look identical to a third-party assembly's foreign, never-checked-out document.
            // Whether a path falls under our workdir is existence-independent and distinguishes
            // the two correctly: a document we own can vanish and still be "ours" to worry about,
            // while a NuGet package's original build-machine path never is.
            var relativePath = Path.GetRelativePath(Workdir.FullName, document.Url);
            return !relativePath.StartsWith("..") && !Path.IsPathRooted(relativePath);
        }
    }
}
