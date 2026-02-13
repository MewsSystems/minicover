using System;
using System.Collections.Generic;
using System.IO;

namespace MiniCover.HitServices
{
    /// <summary>
    /// Stores HitContext references (not copies). Flush serializes the final state
    /// of each context, including any hits recorded after Save.
    /// </summary>
    public class InMemoryHitContextStorage : IHitContextStorage
    {
        private readonly Dictionary<string, HitContext> _storage = new Dictionary<string, HitContext>();

        public void Save(HitContext hitContext, string hitsPath)
        {
            lock (_storage)
            {
                var fileName = Path.Combine(hitsPath, $"{hitContext.Id}.hits");
                _storage[fileName] = hitContext;
            }
        }

        public bool Clear(string hitsPath)
        {
            lock (_storage)
            {
                _storage.Clear();
                return true;
            }
        }

        /// <summary>
        /// Writes all stored contexts to disk and clears the dictionary.
        /// The lock is held during I/O intentionally — Flush runs once after tests complete,
        /// not concurrently with Save. On partial failure, already-written entries remain in
        /// the dictionary (FileMode.Create makes rewrites idempotent), so Flush can be retried.
        /// </summary>
        public void Flush()
        {
            lock (_storage)
            {
                foreach (var kvp in _storage)
                {
                    var fileName = kvp.Key;
                    var path = Path.GetDirectoryName(fileName) ??
                               throw new InvalidOperationException($"Cannot get directory name for {fileName}.");
                    Directory.CreateDirectory(path);

                    using (var fileStream = File.Open(fileName, FileMode.Create))
                    {
                        kvp.Value.Serialize(fileStream);
                        fileStream.Flush();
                    }
                }

                _storage.Clear();
            }
        }
    }
}
