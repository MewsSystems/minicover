using System;
using System.Collections.Generic;
using System.IO;

namespace MiniCover.HitServices
{
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
