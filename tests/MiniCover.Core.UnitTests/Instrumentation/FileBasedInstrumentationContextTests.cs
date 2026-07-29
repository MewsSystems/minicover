using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using MiniCover.Core.Instrumentation;
using Mono.Cecil.Cil;
using Xunit;

namespace MiniCover.Core.UnitTests.Instrumentation
{
    public class FileBasedInstrumentationContextTests
    {
        private readonly IFileSystem _fileSystem = new System.IO.Abstractions.FileSystem();
        private readonly string _workdir = Path.Join(Path.GetTempPath(), "minicover-workdir-tests");

        [Fact]
        public void IsKnownDocument_WithDocumentUnderWorkdir_ReturnsTrue()
        {
            var context = CreateContext();

            context.IsKnownDocument(new Document(Path.Join(_workdir, "Foo.cs"))).Should().BeTrue();
        }

        [Fact]
        public void IsKnownDocument_WithDocumentOutsideWorkdir_ReturnsFalse()
        {
            var context = CreateContext();
            var outside = Path.Join(Path.GetTempPath(), "minicover-workdir-tests-sibling", "Foo.cs");

            context.IsKnownDocument(new Document(outside)).Should().BeFalse();
        }

        [Fact]
        public void IsKnownDocument_WithNonRootedDocumentUrl_ReturnsFalse()
        {
            var context = CreateContext();

            context.IsKnownDocument(new Document("some/relative/path.cs")).Should().BeFalse();
        }

        [Fact]
        public void IsKnownDocument_WithWorkdirSubdirectoryNameStartingWithDotDot_ReturnsTrue()
        {
            var context = CreateContext();

            context.IsKnownDocument(new Document(Path.Join(_workdir, "..cache", "Foo.cs"))).Should().BeTrue();
        }

        private FileBasedInstrumentationContext CreateContext()
        {
            return new FileBasedInstrumentationContext
            {
                Workdir = _fileSystem.DirectoryInfo.New(_workdir)
            };
        }
    }
}
