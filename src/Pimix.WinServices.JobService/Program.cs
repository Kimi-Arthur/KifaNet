using System.ServiceProcess;

namespace Pimix.WinServices.JobService {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] {
                new JobService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
