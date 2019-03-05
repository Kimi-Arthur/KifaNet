using System.Collections.Generic;

namespace Pimix.Service {
    public class PimixServiceJsonClient : PimixServiceClient {
        public static string DataFolder { get; set; }

        public override TDataModel Get<TDataModel>(string id) => throw new System.NotImplementedException();

        public override void Create<TDataModel>(TDataModel data, string id = null) {
            throw new System.NotImplementedException();
        }

        public override void Update<TDataModel>(TDataModel data, string id = null) {
            throw new System.NotImplementedException();
        }

        public override void Delete<TDataModel>(string id) {
            throw new System.NotImplementedException();
        }

        public override void Link<TDataModel>(string targetId, string linkId) {
            throw new System.NotImplementedException();
        }

        public override TResponse Call<TDataModel, TResponse>(string action, string id = null,
            Dictionary<string, object> parameters = null) => throw new System.NotImplementedException();
    }
}
