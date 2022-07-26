using DK.AppEnvironment;
using System.Collections.Generic;
using System.IO;

namespace DK.Implementation.Windows
{
    public class WindowsFileSystem : IFileSystem
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string pathName) => File.Exists(pathName);

        public string CombinePath(string parentPath, string childPath) => Path.Combine(parentPath, childPath);

        public string CombinePath(params string[] pathComponents) => Path.Combine(pathComponents);

        public string GetFullPath(string path) => Path.GetFullPath(path);

        public string GetParentDirectoryName(string path) => Path.GetDirectoryName(path);

        public string GetFileName(string pathName) => Path.GetFileName(pathName);

        public string GetExtension(string pathName) => Path.GetExtension(pathName);

        public string GetFileNameWithoutExtension(string pathName) => Path.GetFileNameWithoutExtension(pathName);

        public IEnumerable<string> GetFilesInDirectory(string path) => Directory.GetFiles(path);

        public IEnumerable<string> GetDirectoriesInDirectory(string path) => Directory.GetDirectories(path);

        public char[] GetInvalidPathChars() => Path.GetInvalidPathChars();

        public char[] GetInvalidFileNameChars() => Path.GetInvalidFileNameChars();

        public bool IsDirectoryHiddenOrSystem(string path) => (new DirectoryInfo(path).Attributes & (FileAttributes.System | FileAttributes.Hidden)) != 0;

        public string GetFileText(string pathName) => File.ReadAllText(pathName);

        public byte[] GetFileBytes(string pathName) => File.ReadAllBytes(pathName);

        public void WriteFileText(string pathName, string text) => File.WriteAllText(pathName, text);

        public void WriteFileBytes(string pathName, byte[] data) => File.WriteAllBytes(pathName, data);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    }
}
