using System.ServiceProcess;

namespace Pimix.WinServices.JobService {
    public partial class JobService : ServiceBase {
        public JobService() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {
        }

        protected override void OnStop() {
        }
    }
}
