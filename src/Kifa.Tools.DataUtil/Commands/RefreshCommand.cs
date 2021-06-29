using CommandLine;
using Kifa.Infos;
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

            switch (type) {
                case TvShow.ModelId:
                    return (int) new DataChef<TvShow, TvShowRestServiceClient>().Refresh(id).Status;
                case Anime.ModelId:
                    return (int) new DataChef<Anime, KifaServiceRestClient<Anime>>().Refresh(id).Status;
                default:
                    logger.Error($"Unknown type name: {type}.");
                    return 1;
            }
        }
    }
}
