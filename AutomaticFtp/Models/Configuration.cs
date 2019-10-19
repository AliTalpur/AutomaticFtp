using System;
using System.Configuration;

namespace AutomaticFTP.Models
{
    public class Configuration
    {
        private string _watchDirectory = ConfigurationManager.AppSettings["WatchDirectory"];
        private string _ftpAddress = ConfigurationManager.AppSettings["FtpAddress"];
        private string _username = ConfigurationManager.AppSettings["Username"];
        private string _password = ConfigurationManager.AppSettings["Password"];
        private string _spreadSheetId = ConfigurationManager.AppSettings["SpreadSheetId"];
        private string _spreadSheetRange = ConfigurationManager.AppSettings["SpreadSheetRange"];

        public Configuration()
        {
            if (string.IsNullOrEmpty(_watchDirectory))
                throw new Exception("Watch Directory not set.");
            if (string.IsNullOrEmpty(_ftpAddress))
                throw new Exception("Ftp Address not set.");
            if (string.IsNullOrEmpty(_username))
                throw new Exception("Username not set.");
            if (string.IsNullOrEmpty(_password))
                throw new Exception("Password not set.");
            if (string.IsNullOrEmpty(_spreadSheetId))
                throw new Exception("Spreadsheet ID not set.");
            if (string.IsNullOrEmpty(_spreadSheetRange))
                throw new Exception("Spreadsheet Range not set.");
        }
    }
}
