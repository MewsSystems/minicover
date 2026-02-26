using System;
using System.IO;
using System.Linq;

namespace MiniCover.HitServices
{
    public class FileHitContextStorage : IHitContextStorage
    {
        public void Save(HitContext hitContext, string hitsPath)
        {
            Directory.CreateDirectory(hitsPath);
            var fileName = Path.Combine(hitsPath, $"{hitContext.Id}.hits");
            using (var fileStream = File.Open(fileName, FileMode.Create))
            {
                hitContext.Serialize(fileStream);
                fileStream.Flush();
            }
        }

        public bool Clear(string hitsPath)
        {
            var hitsDirectory = new DirectoryInfo(hitsPath);

            var hitsFiles = hitsDirectory.Exists
                ? hitsDirectory.GetFiles("*.hits")
                : Array.Empty<FileInfo>();

            if (!hitsFiles.Any())
                return true;

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

            return errorsCount == 0;
        }
    }
}
