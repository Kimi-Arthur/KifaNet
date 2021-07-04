using CommandLine;
using Kifa.Service;
using NLog;

namespace Kifa.Tools.DataUtil.Commands {
    [Verb("refresh", HelpText = "Refresh Data for an entity. Currently tv_shows and animes are supported.")]
    class RefreshCommand : KifaCommand {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [Value(0, Required = true, HelpText = "Entity to refresh.")]
        public string EntityId { get; set; }

        public override int Execute() {
            var segments = EntityId.Split('/');
            var type = segments[0];
            var id = segments[1];

            var chef = DataChef.GetChef(type);

            if (chef == null) {
                logger.Error($"Unknown type name: {type}.");
                return 1;
            }

            return (int) logger.LogResult(chef.Refresh(id), "Summary").Status;
        }
    }
}
