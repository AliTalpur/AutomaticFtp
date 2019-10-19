﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;

namespace AutomaticFTP.Models
{
    public interface IDataSource
    {
        void InitialiseDataSource();
        void WriteToDataSource(string fileName, string status);
        void ReadFromDataSource();
    }

    public class GoogleSheetsDataSource : IDataSource
    {
        private string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private string ApplicationName = "AutomaticFtp";
        private SheetsService service;

        public void InitialiseDataSource()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                //Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public void ReadFromDataSource()
        {
            throw new NotImplementedException();
        }

        public void WriteToDataSource(string fileName, string status)
        {
            var rowValues = new List<object>() { fileName, status, DateTime.Now.ToString("MM/dd/yyyy hh:mm tt") };
            ValueRange vr = new ValueRange
            {
                Values = new List<IList<object>> { rowValues }
            };

            var spreadsheetId = ConfigurationManager.AppSettings["SpreadSheetId"];
            var range = ConfigurationManager.AppSettings["SpreadSheetRange"];

            var request = service.Spreadsheets.Values.Append(vr, spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            var response = request.Execute();
        }
    }
}
