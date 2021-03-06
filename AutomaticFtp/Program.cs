﻿using AutomaticFtp.Models;
using SimpleInjector;
using System.ServiceProcess;

namespace AutomaticFtp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        private static Container container;

        static void Main()
        {
            RegisterContainer();

            var ftpService = container.GetInstance<IFtpService>();
            var directoryService = container.GetInstance<IDirectoryService>();

#if DEBUG
            //While debugging this section is used.
            ftpService.OnDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                ftpService as FtpService
            };
            ServiceBase.Run(ServicesToRun);
            //ftpService.OnStart();
#endif
        }

        static void RegisterContainer()
        {
            container = new Container();

            container.Register<IFtpService, FtpService>(Lifestyle.Singleton);
            container.Register<IDataSource, GoogleSheetsDataSource>(Lifestyle.Singleton);

            // TODO - Unable to turn to scoped.
            container.Register<IDirectoryService, DirectoryService>(Lifestyle.Singleton);

            container.Verify();
        }
    }
}
