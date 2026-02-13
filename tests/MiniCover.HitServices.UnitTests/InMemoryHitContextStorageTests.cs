using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MiniCover.HitServices.UnitTests
{
    // These tests use the real file system (temp directories) because Flush is inherently
    // a disk operation. MiniCover.HitServices targets netstandard2.0 and is embedded in
    // measured assemblies, so adding a System.IO.Abstractions dependency just for mocking
    // would be too invasive. Real file system is acceptable here — the tests are isolated
    // (random temp dirs) and self-cleaning (try/finally).
    public class InMemoryHitContextStorageTests
    {
        private readonly InMemoryHitContextStorage _sut = new InMemoryHitContextStorage(new FileHitContextStorage());

        [Fact]
        public void FlushShouldWriteHitsFilesToDisk()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                                var context = new HitContext("asm", "cls", "method", new Dictionary<int, int> { { 1, 5 } });

                _sut.Save(context, tmpDir);
                Directory.Exists(tmpDir).Should().BeFalse("Save should not write to disk");

                _sut.Flush();

                var files = Directory.GetFiles(tmpDir, "*.hits");
                files.Should().HaveCount(1);

                using (var stream = File.OpenRead(files[0]))
                {
                    var deserialized = HitContext.Deserialize(stream).ToArray();
                    deserialized.Should().HaveCount(1);
                    deserialized[0].Hits.Should().BeEquivalentTo(context.Hits);
                }
            }
            finally
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
        }

        [Fact]
        public void FlushShouldClearStorageAfterWriting()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                                _sut.Save(new HitContext("asm", "cls", "m", new Dictionary<int, int> { { 1, 1 } }), tmpDir);

                _sut.Flush();

                // Second flush should produce no additional files
                var filesBefore = Directory.GetFiles(tmpDir, "*.hits");
                foreach (var f in filesBefore) File.Delete(f);

                _sut.Flush();

                Directory.GetFiles(tmpDir, "*.hits").Should().BeEmpty();
            }
            finally
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
        }

        [Fact]
        public void SaveShouldOverwriteDuplicateContext()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                                var context = new HitContext("asm", "cls", "m", new Dictionary<int, int> { { 1, 1 } });

                _sut.Save(context, tmpDir);
                context.RecordHit(2);
                _sut.Save(context, tmpDir);

                _sut.Flush();

                var files = Directory.GetFiles(tmpDir, "*.hits");
                files.Should().HaveCount(1, "same context id should produce one file");

                using (var stream = File.OpenRead(files[0]))
                {
                    var deserialized = HitContext.Deserialize(stream).Single();
                    deserialized.Hits.Should().ContainKey(2);
                }
            }
            finally
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
        }

        [Fact]
        public void ClearShouldDiscardPendingHits()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                                _sut.Save(new HitContext("asm", "cls", "m", new Dictionary<int, int> { { 1, 1 } }), tmpDir);

                _sut.Clear(tmpDir);
                _sut.Flush();

                Directory.Exists(tmpDir).Should().BeFalse("nothing should be flushed after clear");
            }
            finally
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
        }

        [Fact]
        public void FlushShouldHandleMultipleContexts()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            try
            {
                                var c1 = new HitContext("asm", "cls", "m1", new Dictionary<int, int> { { 1, 1 } });
                var c2 = new HitContext("asm", "cls", "m2", new Dictionary<int, int> { { 2, 2 } });

                _sut.Save(c1, tmpDir);
                _sut.Save(c2, tmpDir);
                _sut.Flush();

                Directory.GetFiles(tmpDir, "*.hits").Should().HaveCount(2);
            }
            finally
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
        }
    }
}
