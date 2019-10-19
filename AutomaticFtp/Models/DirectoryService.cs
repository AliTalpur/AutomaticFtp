using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AutomaticFtp.Models
{
    public interface IDirectoryService
    {
        string GetSubdirectoryFromFilePath(string filePath);
        string RemoveWatchDirectory(string directoryPath);
    }
    
    public class DirectoryService : IDirectoryService
    {
        private string _watchDirectory = ConfigurationManager.AppSettings["WatchDirectory"];
        private List<string> _subdirectories = new List<string>();

        public DirectoryService()
        {
            InitalizeSubdirectories();
        }

        private void InitalizeSubdirectories()
        {
            var directoryInfo = new DirectoryInfo(_watchDirectory).GetDirectories();

            foreach (var directory in directoryInfo)
                _subdirectories.Add(RemoveWatchDirectory(directory.FullName));
        }

        public string GetSubdirectoryFromFilePath(string filePath)
        {
            var subdirectoryName = RemoveWatchDirectory(Path.GetDirectoryName(filePath));

            var subDirectory = _subdirectories.FirstOrDefault(sb => sb == subdirectoryName);

            return subDirectory.Replace('\\', '/');
        }

        public string RemoveWatchDirectory(string directoryPath)
        {
            return directoryPath.Replace(_watchDirectory, "");
        }
    }
}
