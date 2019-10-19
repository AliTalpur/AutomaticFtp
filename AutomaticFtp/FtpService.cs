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
using AutomaticFtp.Models;

namespace AutomaticFtp
{
    public interface IFtpService
    {
        void OnDebug();
        void OnStart();
    }

    public partial class FtpService : ServiceBase, IFtpService
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _watchDirectory = ConfigurationManager.AppSettings["WatchDirectory"];
        private string _ftpAddress = ConfigurationManager.AppSettings["FtpAddress"];
        private string _username = ConfigurationManager.AppSettings["Username"];
        private string _password = ConfigurationManager.AppSettings["Password"];

        private List<string> _subdirectories = new List<string>();
        private IDataSource _dataSource;
        private IDirectoryService _directoryService;

        public FtpService(IDataSource dataSource, IDirectoryService directoryService)
        {
            _dataSource = dataSource;
            _directoryService = directoryService;
        }

        public void OnDebug()
        {
            OnStart();
        }

        public void OnStart()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            _log.Info("Starting Service.");
            //InitializeComponent();

            _log.Info("Creating DataSource.");
            //_dataSource.InitialiseDataSource();

            _log.Info("Starting new Thread for Listener.");
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
            _log.Info("Begin Ftp Transfer.");

            try
            {
                FileInfo fInfo = new FileInfo(e.FullPath);
                while (IsFileLocked(fInfo))
                {
                    _log.Info("File still downloading...");
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

                _log.Info("Copying File Contents.");
                var fileContents = CopyContents(e.FullPath);

                _log.Info("Uploading File.");
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }
                _log.Info("File Uploaded.");

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    //_dataSource.WriteToDataSource(category, fileName);
                    _log.Info($"Upload File Complete, status {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception occurred, {ex}");
            }
            
        }

        private string ConfigureFtpAddress(string filePath)
        {
            // Check for subdirectory, adjust path accordingly.
            var subdirectory = _directoryService.GetSubdirectoryFromFilePath(filePath);
            _log.Info($"Subdirectory : {subdirectory}");

            return _ftpAddress + subdirectory;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void ListenToFolder()
        {
            _log.Info("Creating FileSystemWatcher.");
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

            _log.Info("Creating FileSystemWatcher Completed.");
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
