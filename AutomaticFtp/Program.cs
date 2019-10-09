using AutomaticFTP.Models;
using SimpleInjector;

namespace AutomaticFTP
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


#if DEBUG
            //While debugging this section is used.
            var myService = container.GetInstance<IFtpService>();
            //var myService = new FtpService();
            myService.onDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

        static void RegisterContainer()
        {
            container = new Container();

            container.Register<IDataSource, GoogleSheetsDataSource>(Lifestyle.Singleton);
            container.Register<IFtpService, FtpService>(Lifestyle.Singleton);

            container.Verify();
        }
    }
}
