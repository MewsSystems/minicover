using System;
using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using MiniCover.Core.Extensions;
using Mono.Cecil.Cil;
using Xunit;

namespace MiniCover.UnitTests.Extensions
{
    public class DocumentExtensionsTests : IDisposable
    {
        private readonly string _file;

        public DocumentExtensionsTests()
        {
            _file = Path.Join(Path.GetTempPath(), $"minicover-{Environment.CurrentManagedThreadId}-{Environment.TickCount64}.cs");
            File.WriteAllText(_file, "class C {}");
        }

        public void Dispose()
        {
            File.Delete(_file);
        }

        [Fact]
        public void FileHasChanged_WhenFileMissing_ReturnsTrue()
        {
            var document = new Document(Path.Join(Path.GetTempPath(), "does-not-exist.cs"))
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };

            document.FileHasChanged().Should().BeTrue();
        }

        // A compiler sentinel for synthesized code - F# names one 'unknown'. It corresponds to no
        // file on any machine, so it is not a change and must not be reported as one just because
        // no such file exists.
        [Fact]
        public void FileHasChanged_WhenDocumentHasNoChecksumAndFileMissing_ReturnsFalse()
        {
            var document = new Document("unknown")
            {
                HashAlgorithm = DocumentHashAlgorithm.None,
                Hash = Array.Empty<byte>()
            };

            document.FileHasChanged().Should().BeFalse();
        }

        [Fact]
        public void FileHasChanged_WhenDocumentHasHashAlgorithmButNoHashAndFileMissing_ReturnsFalse()
        {
            var document = new Document(Path.Join(Path.GetTempPath(), "does-not-exist.cs"))
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = Array.Empty<byte>()
            };

            document.FileHasChanged().Should().BeFalse();
        }

        // Generated documents are checksummed but live only in the PDB, so a missing file is expected
        // rather than a change. Deliberately not named *.g.cs: the embedded source is what makes the
        // document benign, so the rule has to hold for a name the old exemption never covered.
        [Fact]
        public void FileHasChanged_WhenMissingFileHasSourceEmbeddedInPdb_ReturnsFalse()
        {
            var document = new Document(Path.Join(Path.GetTempPath(), "obj", "generated", "Template.generated.cs"))
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };
            EmbedSource(document, "class C {}");

            document.FileHasChanged().Should().BeFalse();
        }

        // The old rule exempted every missing *.g.cs by name. The embedded source is what makes a
        // missing document benign, not its file extension.
        [Fact]
        public void FileHasChanged_WhenMissingGeneratedFileHasNoEmbeddedSource_ReturnsTrue()
        {
            var document = new Document(Path.Join(Path.GetTempPath(), "does-not-exist.g.cs"))
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };

            document.FileHasChanged().Should().BeTrue();
        }

        [Fact]
        public void FileHasChanged_WhenHashMatchesCurrentContent_ReturnsFalse()
        {
            var document = new Document(_file)
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };

            document.FileHasChanged().Should().BeFalse();
        }

        [Fact]
        public void FileHasChanged_WhenContentWasModifiedSinceHashWasRecorded_ReturnsTrue()
        {
            var document = new Document(_file)
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };

            File.WriteAllText(_file, "class C { void M() {} }");

            document.FileHasChanged().Should().BeTrue();
        }

        // Embedded source excuses a missing file, not a file on disk that no longer matches.
        [Fact]
        public void FileHasChanged_WhenContentWasModifiedAndSourceIsEmbeddedInPdb_ReturnsTrue()
        {
            var document = new Document(_file)
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };
            EmbedSource(document, "class C {}");

            File.WriteAllText(_file, "class C { void M() {} }");

            document.FileHasChanged().Should().BeTrue();
        }

        [Fact]
        public void FileHasChanged_WhenHashAlgorithmIsUnknown_ReturnsFalse()
        {
            var document = new Document(_file)
            {
                HashAlgorithm = DocumentHashAlgorithm.None,
                Hash = ComputeHash("class C {}")
            };

            document.FileHasChanged().Should().BeFalse();
        }

        private static void EmbedSource(Document document, string content)
        {
            document.CustomDebugInformations.Add(new EmbeddedSourceDebugInformation(
                System.Text.Encoding.UTF8.GetBytes(content),
                compress: false));
        }

        private static byte[] ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        }
    }
}
