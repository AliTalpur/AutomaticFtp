using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using AutomaticFTP.Models;

namespace AutomaticFTP
{
    public interface IFtpService
    {
        void OnDebug();
        void OnStart();
    }

    public partial class FtpService : ServiceBase, IFtpService
    {
        private string _watchDirectory = ConfigurationManager.AppSettings["WatchDirectory"];
        private string _ftpAddress = ConfigurationManager.AppSettings["FtpAddress"];
        private string _username = ConfigurationManager.AppSettings["Username"];
        private string _password = ConfigurationManager.AppSettings["Password"];

        private List<string> _subdirectories = new List<string>();
        private IDataSource _dataSource;

        public FtpService(IDataSource dataSource)
        {
            _dataSource = dataSource;

        }

        public void OnDebug()
        {
            OnStart(null);
        }

        public void OnStart()
        {

            InitializeComponent();
            InitalizeSubdirectories();
            _dataSource.InitialiseDataSource();

            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            ThreadStart work = ListenToFolder;
            Thread thread = new Thread(work);
            thread.Start();
        }

        protected override void OnStop()
        {
        }

        private byte[] CopyContents(string path)
        {
            byte[] fileContents;
            using (StreamReader sourceStream = new StreamReader(path))
            {
                fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            }

            return fileContents;
        }

        private void TransferWithFtp(object source, FileSystemEventArgs e)
        {
            FileInfo fInfo = new FileInfo(e.FullPath);
            while (IsFileLocked(fInfo))
            {
                Thread.Sleep(500);
            }

            var directorySplit = e.Name.Split('\\');
            var category = directorySplit[0];
            var fileName = directorySplit[1];

            var absoluteFtpUri = ConfigureFtpAddress(e.FullPath) + "/" + fileName;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(absoluteFtpUri);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            request.Credentials =
                new NetworkCredential(_username, _password);

            var fileContents = CopyContents(e.FullPath);

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                _dataSource.WriteToDataSource(category, fileName);
                Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }
        }

        private string ConfigureFtpAddress(string filePath)
        {
            // Check for subdirectory, adjust path accordingly.
            var subdirectory = GetSubdirectoryFromFilePath(filePath);
            subdirectory = subdirectory.Replace('\\', '/');

            return _ftpAddress + subdirectory;
        }

        private void InitalizeSubdirectories()
        {
            var directoryInfo = new DirectoryInfo(_watchDirectory).GetDirectories();

            foreach(var directory in directoryInfo)
                _subdirectories.Add(RemoveWatchDirectory(directory.FullName));
        }

        private string GetSubdirectoryFromFilePath(string filePath)
        {
            var subdirectoryName = RemoveWatchDirectory(Path.GetDirectoryName(filePath));
            
            return _subdirectories.FirstOrDefault(sb => sb == subdirectoryName);
        }

        private string RemoveWatchDirectory(string directoryPath)
        {
            return directoryPath.Replace(_watchDirectory, "");
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void ListenToFolder()
        {
            var watcher = new FileSystemWatcher
            {
                Path = _watchDirectory,

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName,

                // Only watch text files.
                Filter = "*.nzb",

                // Include Subdirectories
                IncludeSubdirectories = true
            };

            // Add event handlers.
            watcher.Created += TransferWithFtp;
            // watcher.Changed += TransferWithFtp;
            // watcher.Deleted += TransferWithFtp;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
    }
}
