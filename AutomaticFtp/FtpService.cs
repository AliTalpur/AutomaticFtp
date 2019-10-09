using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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
        void onDebug();
    }

    public partial class FtpService : ServiceBase, IFtpService
    {
        private List<string> SubDirectories;
        private IDataSource _dataSource;

        public FtpService(IDataSource dataSource)
        {
            _dataSource = dataSource;

            InitializeComponent();
        }

        public void onDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            Init();
            ListenToFolder();
        }

        protected override void OnStop()
        {
        }

        private void Init()
        {
            _dataSource.InitialiseDataSource();
            _dataSource.WriteToDataSource();

            var directories = Directory.GetDirectories(ConfigurationManager.AppSettings["WatchDirectory"]);

            foreach(var directory in directories)
                SubDirectories.Add(directory.Replace(ConfigurationManager.AppSettings["WatchDirectory"] + "\\", ""));
        }

        // Move FTP functionality to its own class.
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
            var ftpAddress = ConfigurationManager.AppSettings["FtpAddress"];

            // Check for subdirectory, adjust path accordingly.

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(string.Concat(ftpAddress, e.Name));
            request.Method = WebRequestMethods.Ftp.UploadFile;

            request.Credentials = 
                new NetworkCredential(ConfigurationManager.AppSettings["Username"], ConfigurationManager.AppSettings["Password"]);

            var fileContents = CopyContents(e.FullPath);

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void ListenToFolder()
        {
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                // TODO - Config
                watcher.Path = ConfigurationManager.AppSettings["WatchDirectory"];

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watcher.NotifyFilter = NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName
                                     | NotifyFilters.DirectoryName;

                // Only watch text files.
                watcher.Filter = "*.txt";

                // Include Subdirectories
                watcher.IncludeSubdirectories = true;

                // Add event handlers.
                watcher.Created += TransferWithFtp;
                // watcher.Changed += TransferWithFtp;
                // watcher.Deleted += TransferWithFtp;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                Console.WriteLine("Press 'q' to quit the sample.");
                while (Console.Read() != 'q') ;
            }
        }
    }
}
