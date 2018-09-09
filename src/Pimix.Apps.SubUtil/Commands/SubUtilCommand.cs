using Pimix.Configs;

namespace Pimix.Apps.SubUtil.Commands {
    abstract class SubUtilCommand {
        public abstract int Execute();

        public void Initialize() {
            PimixConfigs.LoadFromSystemConfigs();
        }
    }
}
