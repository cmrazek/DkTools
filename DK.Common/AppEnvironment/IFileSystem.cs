using System.Collections.Generic;

namespace DK.AppEnvironment
{
    public interface IFileSystem
    {
        bool DirectoryExists(string path);

        bool FileExists(string pathName);

        string CombinePath(string parentPath, string childPath);

        string CombinePath(params string[] pathComponents);

        string GetFullPath(string path);

        string GetParentDirectoryName(string path);

        string GetFileName(string pathName);

        string GetExtension(string pathName);

        string GetFileNameWithoutExtension(string pathName);

        IEnumerable<string> GetFilesInDirectory(string path);

        IEnumerable<string> GetDirectoriesInDirectory(string path);

        char[] GetInvalidPathChars();

        char[] GetInvalidFileNameChars();

        bool IsDirectoryHiddenOrSystem(string path);
    }
}
