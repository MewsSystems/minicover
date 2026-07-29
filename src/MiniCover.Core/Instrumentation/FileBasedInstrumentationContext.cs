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
            // A non-rooted document.Url (e.g. a Windows-authored path on Linux, where it isn't
            // recognized as rooted) must not reach GetRelativePath: it resolves non-rooted paths
            // against the process's current directory rather than against Workdir, which would
            // wrongly resolve it to somewhere inside Workdir. Our own Sources/Tests are always
            // rooted, so a non-rooted document.Url can never be one of ours.
            if (!Path.IsPathRooted(document.Url))
                return false;

            // Existence-independent on purpose: Sources/Tests only list files that currently
            // exist, which would wrongly exclude a workdir file that's simply missing right now.
            var relativePath = Path.GetRelativePath(Workdir.FullName, document.Url);
            return relativePath != ".."
                && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar)
                && !Path.IsPathRooted(relativePath);
        }
    }
}
