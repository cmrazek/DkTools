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

        string GetFileText(string pathName);

        byte[] GetFileBytes(string pathName);

        void WriteFileText(string pathName, string text);

        void WriteFileBytes(string pathName, byte[] data);

        void CreateDirectory(string path);
    }
}
