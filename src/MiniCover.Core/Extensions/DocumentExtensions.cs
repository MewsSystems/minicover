using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil.Cil;

namespace MiniCover.Core.Extensions
{
    public static class DocumentExtensions
    {
        public static bool FileHasChanged(this Document document)
        {
            // The checksum, not the file's presence, decides whether a document can be verified
            // at all - so it has to be asked first. A document with no recorded checksum matches
            // no file on any machine: it is a compiler sentinel (like the checksum-less document
            // named 'unknown' that the F# compiler in .NET SDK 10.0.400 emits), so it can never
            // count as a change. Checking existence first reported it as changed and skipped the
            // whole assembly.
            using var hasher = CreateHashAlgorithm(document.HashAlgorithm);

            if (hasher == null || document.Hash == null || document.Hash.Length == 0)
                return false;

            // A checksummed document is a real file the compiler read, so its current content
            // decides. Deleting or editing it is a change, which is what keeps a stale assembly
            // out of the coverage report.
            if (File.Exists(document.Url))
            {
                using var stream = File.OpenRead(document.Url);
                var newHash = hasher.ComputeHash(stream);
                return !newHash.SequenceEqual(document.Hash);
            }

            // Source-generated documents (obj/.../*.g.cs and friends) are checksummed but never
            // expected on disk - the compiler carries their content in the PDB instead. Nothing
            // can have changed underneath them.
            return !document.HasEmbeddedSource();
        }

        private static bool HasEmbeddedSource(this Document document)
        {
            return document.CustomDebugInformations.Any(info => info is EmbeddedSourceDebugInformation);
        }

        private static HashAlgorithm CreateHashAlgorithm(DocumentHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case DocumentHashAlgorithm.SHA1: return SHA1.Create();
                case DocumentHashAlgorithm.SHA256: return SHA256.Create();
                case DocumentHashAlgorithm.MD5: return MD5.Create();
                default: return null;
            }
        }
    }
}
