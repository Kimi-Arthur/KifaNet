using Newtonsoft.Json;
using Pimix;
using Pimix.Infos;
using Pimix.Service;
using Xunit;

namespace PimixTest.Infos {
    public class TvShowTests {
        public PimixServiceClient Client { get; set; }

        public TvShowTests() {
            PimixServiceRestClient.PimixServerApiAddress = "http://www.pimix.tk/api";
            Client = new PimixServiceRestClient();
        }

        [Fact]
        public void Get() {
            var show = Client.Get<TvShow>("信長協奏曲");
            var s = JsonConvert.SerializeObject(show, Defaults.JsonSerializerSettings);
            Assert.Equal("信長協奏曲", show.Id);
            Assert.Equal(Region.Japan, show.Region);
            Assert.Equal(Language.Japanese, show.Language);
        }
    }
}
