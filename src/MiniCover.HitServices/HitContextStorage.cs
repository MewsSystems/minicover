using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiniCover.HitServices
{
    public static class HitContextStorage
    {
        private static readonly Dictionary<string, MemoryStream> _storage = new Dictionary<string, MemoryStream>();

        public static void Save(HitContext hitContext, string hitsPath)
        {
            lock (_storage)
            {
                var fileName = Path.Combine(hitsPath, $"{hitContext.Id}.hits");

                if (!_storage.TryGetValue(fileName, out var stream))
                {
                    stream = new MemoryStream();
                    _storage[fileName] = stream;
                }

                hitContext.Serialize(stream);
                stream.Flush();
            }
        }

        public static void Flush()
        {
            lock (_storage)
            {
                foreach (var kvp in _storage)
                {
                    var fileName = kvp.Key;
                    var path = Path.GetDirectoryName(fileName) ??
                               throw new InvalidOperationException($"Cannot get directory name for {fileName}.");
                    Directory.CreateDirectory(path);

                    var memoryStream = kvp.Value;

                    using (var fileStream = File.Open(fileName, FileMode.Append))
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }

                    memoryStream.Dispose();
                }

                _storage.Clear();
            }
        }

        public static bool Clear(string hitsPath)
        {
            lock (_storage)
            {
                foreach (var kvp in _storage)
                {
                    kvp.Value.Dispose();
                }
                var hitsDirectory = new DirectoryInfo(hitsPath);

                var hitsFiles = hitsDirectory.Exists
                    ? hitsDirectory.GetFiles("*.hits")
                    : Array.Empty<FileInfo>();
                _storage.Clear();

                if (!hitsFiles.Any())
                {
                    return true;
                }

                var errorsCount = 0;
                foreach (var hitsFile in hitsFiles)
                {
                    try
                    {
                        hitsFile.Delete();
                    }
                    catch (Exception)
                    {
                        errorsCount++;
                    }
                }

                if (errorsCount != 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
