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

        [Fact]
        public void FileHasChanged_WhenMissingGeneratedFile_ReturnsFalse()
        {
            var document = new Document(Path.Join(Path.GetTempPath(), "does-not-exist.g.cs"))
            {
                HashAlgorithm = DocumentHashAlgorithm.SHA256,
                Hash = ComputeHash("class C {}")
            };

            document.FileHasChanged().Should().BeFalse();
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

        private static byte[] ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        }
    }
}
