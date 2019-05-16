using System.Collections.Generic;
using Pimix.Service;

namespace Pimix.Bilibili {
    public class BilibiliUploader : DataModel {
        public const string ModelId = "bilibili/uploaders";

        static BilibiliUploaderServiceClient client;

        public static BilibiliUploaderServiceClient Client => client =
            client ?? new BilibiliUploaderRestServiceClient();

        public string Name { get; set; }
        public List<string> Aids { get; set; }
    }

    public interface BilibiliUploaderServiceClient : PimixServiceClient<BilibiliUploader> {
    }

    public class BilibiliUploaderRestServiceClient : PimixServiceRestClient<BilibiliUploader>,
        BilibiliUploaderServiceClient {
    }
}
