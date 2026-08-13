using System.IO;

namespace MiniCover.Core.FileSystem
{
    public interface IFileReader
    {
        string[] ReadAllLines(FileInfo file);

        /// <summary>Lines of the file, or null if no such file exists.</summary>
        string[] TryReadAllLines(FileInfo file);
    }
}
